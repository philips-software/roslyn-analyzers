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

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Common.Inspection;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	public class ExceptionWalker
	{
		private const int ThrowOpCode = 0x7a;

		public IEnumerable<string> UnhandledFromInvocation(InvocationExpressionSyntax invocation, IReadOnlyDictionary<string, string> aliases, SemanticModel semanticModel)
		{
			IEnumerable<string> unhandledExceptions = Array.Empty<string>();
			var invokedSymbol = semanticModel.GetSymbolInfo(invocation).Symbol;
			if (invokedSymbol is IMethodSymbol or IPropertySymbol)
			{
				unhandledExceptions = UnhandledExceptionsFromSymbol(invokedSymbol);
			}

			var tryStatements = invocation.Ancestors().OfType<TryStatementSyntax>();
			foreach (var tryStatement in tryStatements)
			{
				var handledExceptionTypes = tryStatement.Catches.Select(cat => cat.Declaration?.Type.GetFullName(aliases));
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
			ModuleDefinition module = ModuleDefinition.ReadModule(assemblyPath);
			var type = module.GetType(fullTypeName);
			var methodDef = type.GetMethods().FirstOrDefault(m => m.Name == symbol.Name);
			CallTreeNode root = CallTreeNode.CreateCallTree(methodDef);
			return UnhandledExceptionsFromCallTree(root);
		}

		
		public IEnumerable<string> UnhandledExceptionsFromCallTree(CallTreeNode root)
		{
			CallTreeIteratorDeepestFirst iterator = new(root);
			IEnumerable<string> lastOpenExceptions = Array.Empty<string>();
			foreach(CallTreeNode node in iterator)
			{
				HashSet<string> openExceptions = new();
				var body = node.Method.Body;
				if (body == null)
				{ 
					node.Tag = Array.Empty<string>();
					continue;
				}

				var catchHandlers =
					body.ExceptionHandlers.Where(handler => handler.HandlerType == ExceptionHandlerType.Catch).ToList();
				var filteredExceptions = new Stack<string>();
				foreach (var instruction in body.Instructions)
				{
					foreach (var filter in catchHandlers)
					{
						if (instruction.Offset == filter.TryStart.Offset)
						{
							filteredExceptions.Push(filter.CatchType.FullName);
						}

						if (instruction.Offset == filter.HandlerStart.Offset)
						{
							filteredExceptions.Pop();
						}
					}

					if (CallTreeNode.IsCallInstruction(instruction))
					{
						var callee = instruction.Operand as MethodDefinition;
						var calleeChild = node.Children.FirstOrDefault(child => child.Method == callee);
						if (calleeChild != null)
						{
							var newExceptions = calleeChild.Tag as IEnumerable<string>;
							foreach (var newException in newExceptions)
							{
								if (!IsThrownExceptionFiltered(newException, filteredExceptions))
								{
									openExceptions.Add(newException);
								}
							}
						}
					}

					if (instruction.OpCode.Op2 == ThrowOpCode)
					{
						string exFullName = GetResultingTypeName(instruction.Previous);
						if (!string.IsNullOrEmpty(exFullName) && !IsThrownExceptionFiltered(exFullName, filteredExceptions))
						{
							openExceptions.Add(exFullName);
						}
					}
				}

				node.Tag = openExceptions;
				lastOpenExceptions = openExceptions;
			}
			return lastOpenExceptions;
		}

		private string GetFullName(ISymbol symbol)
		{
			List<string> namespaces = new();
			var ns = symbol.ContainingNamespace;
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
			if (filteredExceptions.Any(ex => ex == "Exception"))
			{
				return true;
			}
			return filteredExceptions.Contains(thrown);
		}

		private string GetResultingTypeName(Instruction instruction)
		{
			if (CallTreeNode.IsCallInstruction(instruction) && instruction.Operand is MethodDefinition method)
			{
				if (method.IsConstructor)
				{
					return method.DeclaringType.FullName;
				}
				return method.ReturnType.FullName;
			}

			throw new ArgumentException($"No returning type found in: {instruction}");
		}
	}
}
