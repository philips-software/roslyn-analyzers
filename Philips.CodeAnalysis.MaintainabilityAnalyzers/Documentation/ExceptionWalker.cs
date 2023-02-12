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
			HashSet<string> openExceptions = new();
			foreach(CallTreeNode node in iterator)
			{
				var body = node.Method.Body;
				if (body == null)
				{ 
					continue;
				}
				// TODO: Take logic from deeper in the CallTree into account.
				var thrownExceptions = body.Instructions.Where(instr => instr.OpCode.Op2 == ThrowOpCode).Select(thrown => (thrown.Operand as MethodDefinition)?.FullName);
				var caughtExceptions = body.ExceptionHandlers.Where(ex => ex.HandlerType == ExceptionHandlerType.Catch).Select(caught => caught.CatchType.FullName);
				if (caughtExceptions.Any(ex => ex == "Exception"))
				{
					openExceptions.Clear();
				} 
				else
				{
					var addedOpenExceptions = thrownExceptions.Except(caughtExceptions);
					foreach (var toBeAdded in addedOpenExceptions)
					{
						openExceptions.Add(toBeAdded);
					}
				}
			}
			return openExceptions;
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
	}
}
