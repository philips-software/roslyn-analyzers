// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Common.Inspection;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	public class ExceptionWalker
	{
		private const int ThrowOpCode = 0x7a;
		private const int LoadLocalOpCode = 0x06;
		private const int StoreLocalOpCode = 0x0a;

		private static readonly Dictionary<string, IEnumerable<string>> WellKnownMethods = new() {
			// Assuming dotnet checked their resource usage.
			{ StringConstants.GetResourceString, Array.Empty<string>() },
			{ StringConstants.GetExceptionForWin32Error, new [] { StringConstants.IoException, StringConstants.FileNotFoundException, StringConstants.DirectoryNotFoundException, StringConstants.PathTooLongException, StringConstants.UnauthorizedException } },
			{ StringConstants.GetExceptionForLastWin32Error, new [] { StringConstants.IoException, StringConstants.FileNotFoundException, StringConstants.DirectoryNotFoundException, StringConstants.PathTooLongException, StringConstants.UnauthorizedException } },
			{ StringConstants.GetExceptionForIoErrno, new [] { StringConstants.IoException, StringConstants.FileNotFoundException, StringConstants.DirectoryNotFoundException, StringConstants.PathTooLongException, StringConstants.UnauthorizedException } }
		};

		public IEnumerable<string> UnhandledFromInvocation(InvocationExpressionSyntax invocation, IReadOnlyDictionary<string, string> aliases, SemanticModel semanticModel)
		{
			IEnumerable<string> unhandledExceptions = Array.Empty<string>();
			ISymbol invokedSymbol = semanticModel.GetSymbolInfo(invocation).Symbol;
			if (invokedSymbol is IMethodSymbol or IPropertySymbol)
			{
				unhandledExceptions = UnhandledExceptionsFromSymbol(invokedSymbol);
			}

			IEnumerable<TryStatementSyntax> tryStatements = invocation.Ancestors().OfType<TryStatementSyntax>();
			foreach (TryStatementSyntax tryStatement in tryStatements)
			{
				IEnumerable<string> handledExceptionTypes = tryStatement.Catches.Select(cat => cat.Declaration?.Type.GetFullName(aliases));
				unhandledExceptions = handledExceptionTypes.Any(ex => ex == StringConstants.SystemException) ? Array.Empty<string>() : unhandledExceptions.Except(handledExceptionTypes);
			}

			return unhandledExceptions;
		}

		public IEnumerable<string> UnhandledExceptionsFromSymbol(ISymbol symbol)
		{
			var fullTypeName = GetFullName(symbol.ContainingType);
			var assemblyPath = Type.GetType(fullTypeName)?.Assembly.Location;
			if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
			{
				return Array.Empty<string>();
			}
			using var module = ModuleDefinition.ReadModule(assemblyPath);
			TypeDefinition type = module.GetType(fullTypeName);
			MethodDefinition methodDef = type.GetMethods().FirstOrDefault(m => m.Name == symbol.Name);
			var root = CallTreeNode.CreateCallTree(methodDef);
			IEnumerable<string> unhandledExceptions = UnhandledExceptionsFromCallTree(root);
			// For now clear it every time, accuracy before performance.
			CallTreeNode.ClearCache();
			return unhandledExceptions;
		}

		public IEnumerable<string> UnhandledExceptionsFromCallTree(CallTreeNode root)
		{
			CallTreeIteratorDeepestFirst iterator = new(root);
			IEnumerable<string> lastOpenExceptions = Array.Empty<string>();
			foreach (CallTreeNode node in iterator)
			{
				HashSet<string> openExceptions = [];
				if (TrySkipMethod(node, out MethodBody body) || TryGetFromCache(node, ref lastOpenExceptions))
				{
					continue;
				}

				var catchHandlers =
					body.ExceptionHandlers.Where(handler => handler.HandlerType == ExceptionHandlerType.Catch).ToList();
				var filteredExceptions = new Stack<string>();
				foreach (Instruction instruction in body.Instructions)
				{
					AdjustExceptionFilter(catchHandlers, instruction, filteredExceptions);
					HandleCallInstruction(instruction, node, filteredExceptions, openExceptions);
					HandleThrowInstruction(instruction, body, filteredExceptions, openExceptions);
				}

				node.Tag = openExceptions;
				lastOpenExceptions = openExceptions;
			}
			return lastOpenExceptions;
		}

		private static bool TrySkipMethod(CallTreeNode node, out MethodBody body)
		{
			body = node.Method.Body;
			if (body == null)
			{
				node.Tag = Array.Empty<string>();
				return true;
			}

			return false;
		}

		private void HandleThrowInstruction(Instruction instruction, MethodBody body, Stack<string> filteredExceptions,
			HashSet<string> openExceptions)
		{
			if (instruction.OpCode.Op2 == ThrowOpCode)
			{
				TypeDefinition exType = GetResultingTypeName(instruction.Previous, body.Instructions);
				if (exType != null && !IsThrownExceptionFiltered(exType.FullName, filteredExceptions))
				{
					_ = openExceptions.Add(exType.FullName);
				}
			}
		}

		private void HandleCallInstruction(Instruction instruction, CallTreeNode node, Stack<string> filteredExceptions,
			HashSet<string> openExceptions)
		{
			if (CallTreeNode.IsCallInstruction(instruction))
			{
				var callee = instruction.Operand as MethodDefinition;
				CallTreeNode calleeChild = node.Children.FirstOrDefault(child => child.Method == callee);
				if (calleeChild != null)
				{
					var newExceptions = calleeChild.Tag as IEnumerable<string>;
					foreach (var newException in newExceptions)
					{
						if (!IsThrownExceptionFiltered(newException, filteredExceptions))
						{
							_ = openExceptions.Add(newException);
						}
					}
				}
			}
		}

		private static void AdjustExceptionFilter(List<ExceptionHandler> catchHandlers, Instruction instruction, Stack<string> filteredExceptions)
		{
			foreach (ExceptionHandler filter in catchHandlers)
			{
				if (instruction.Offset == filter.TryStart.Offset)
				{
					filteredExceptions.Push(filter.CatchType.FullName);
				}

				if (instruction.Offset == filter.HandlerStart.Offset)
				{
					_ = filteredExceptions.Pop();
				}
			}
		}

		private static bool TryGetFromCache(CallTreeNode node, ref IEnumerable<string> lastOpenExceptions)
		{
			if (WellKnownMethods.TryGetValue(node.Method.FullName, out IEnumerable<string> cached))
			{
				node.Tag = cached;
				lastOpenExceptions = cached;
				return true;
			}

			return false;
		}

		private string GetFullName(ISymbol symbol)
		{
			List<string> namespaces = [];
			INamespaceSymbol ns = symbol.ContainingNamespace;
			while (ns != null && !string.IsNullOrEmpty(ns.Name))
			{
				namespaces.Add(ns.Name);
				ns = ns.ContainingNamespace;
			}
			namespaces.Reverse();
			return $"{string.Join(".", namespaces)}.{symbol.Name}";
		}

		private bool IsThrownExceptionFiltered(string thrown, Stack<string> filteredExceptions)
		{
			if (filteredExceptions.Any(ex => ex == StringConstants.Exception))
			{
				return true;
			}
			return filteredExceptions.Contains(thrown);
		}

		private TypeDefinition GetResultingTypeName(Instruction instruction, Collection<Instruction> instructions)
		{
			TypeDefinition typeDef = null;
			if (CallTreeNode.IsCallInstruction(instruction) && instruction.Operand is MethodDefinition method)
			{
				if (method.IsConstructor)
				{
					typeDef = method.DeclaringType.Resolve();
				}
				else
				{

					if (method.FullName is StringConstants.GetExceptionForLastWin32Error or StringConstants.GetExceptionForWin32Error or StringConstants.GetExceptionForIoErrno)
					{
						return null;
					}

					typeDef = method.ReturnType.Resolve();
				}
			}

			typeDef ??=
				TryGetFromLocalVariable(0, instruction, instructions) ??
				TryGetFromLocalVariable(1, instruction, instructions) ??
				TryGetFromLocalVariable(2, instruction, instructions) ??
				TryGetFromLocalVariable(2 + 1, instruction, instructions);

			if (!IsException(typeDef))
			{
				return null;
			}
			return typeDef;
		}

		private TypeDefinition TryGetFromLocalVariable(int localIndex, Instruction instruction, Collection<Instruction> instructions)
		{
			TypeDefinition typeDef = null;
			if (instruction.OpCode.Op2 == LoadLocalOpCode + localIndex)
			{
				var index = instructions.IndexOf(instruction);
				for (var i = index - 1; i >= 0; i--)
				{
					if (instructions[i].OpCode.Op2 == StoreLocalOpCode + localIndex)
					{
						typeDef = GetResultingTypeName(instructions[i - 1], instructions);
						break;
					}
				}
			}

			return typeDef;
		}

		private bool IsException(TypeDefinition type)
		{
			TypeDefinition typeDef = type;
			while (typeDef != null)
			{
				if (typeDef.FullName == StringConstants.SystemException)
				{
					return true;
				}
				typeDef = typeDef.BaseType?.Resolve();
			}

			return false;
		}
	}
}
