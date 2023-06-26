// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreferReadOnlyParametersAnalyzer : SingleDiagnosticAnalyzer<MethodDeclarationSyntax, PreferReadOnlyParametersNodeAction>
	{
		private const string Title = @"Prefer readonly parameters";
		public const string MessageFormat = @"Make parameter '{0}' a readonly type";
		private const string Description = @"Method parameters that are not written to, can be made readonly";
		public PreferReadOnlyParametersAnalyzer()
			: base(DiagnosticId.PreferReadOnlyParameters, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class PreferReadOnlyParametersNodeAction : SyntaxNodeAction<MethodDeclarationSyntax>
	{
		private static readonly List<string> ReadWriteCollections = new() { "System.Array", "System.Collections.Generic.List", "System.Collections.Generic.IList", "System.Collections.IList", "System.Collections.Generic.Dictionary", "System.Collections.Generic.HashSet", "System.Span" };
		private static readonly List<string> WriteMethods = new() { "Add", "AddRange", "Clear", "Insert", "InsertRange", "Remove", "RemoveAt", "RemoveRange", "RemoveWhere", "Reverse", "Sort" };
		private readonly NamespaceIgnoringComparer _comparer = new();

		public override void Analyze()
		{
			SeparatedSyntaxList<ParameterSyntax> parameters = Node.ParameterList.Parameters;
			if (!parameters.Any())
			{
				return;
			}

			IReadOnlyDictionary<string, string> aliases = Helper.GetUsingAliases(Node);
			IEnumerable<ParameterSyntax> collections = parameters.Where(p => IsReadWriteCollection(p.Type, aliases));
			if (!collections.Any())
			{
				return;
			}
			IEnumerable<MemberAccessExpressionSyntax> accesses = Node.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
			IEnumerable<ElementAccessExpressionSyntax> setters = Node.DescendantNodes()
				.OfType<AssignmentExpressionSyntax>()
				.Select(ass => ass.Left as ElementAccessExpressionSyntax);
			IEnumerable<InvocationExpressionSyntax> invocations = Node.DescendantNodes().OfType<InvocationExpressionSyntax>();
			foreach (ParameterSyntax collectionParameter in collections)
			{
				var parameterName = collectionParameter.Identifier.Text;
				// Calling member that will change the type...
				// Or using the setter of the indexer (example: list[8] = 42) ...
				// Or invocations that require the type
				if (
					!accesses.Any(acc => IsCallingParameter(acc.Expression, parameterName) && IsModifyingMember(acc.Name)) &&
					!setters.Any(element => element != null && IsCallingParameter(element.Expression, parameterName)) &&
					!invocations.Any(voc => RequiresReadWrite(voc, parameterName)))
				{
					Location location = collectionParameter.Type.GetLocation();
					ReportDiagnostic(location, collectionParameter.Identifier.Text);
				}
			}
		}

		private bool IsReadWriteCollection(TypeSyntax type, IReadOnlyDictionary<string, string> aliases)
		{
			var fullName = type?.GetFullName(aliases);
			return ReadWriteCollections.Exists(col => _comparer.Compare(fullName, col) == 0);
		}

		private bool IsCallingParameter(ExpressionSyntax expression, string name)
		{
			return expression is IdentifierNameSyntax identifierName && _comparer.Compare(identifierName.Identifier.Text, name) == 0;
		}

		private static bool IsModifyingMember(SimpleNameSyntax name)
		{
			var nameString = name.Identifier.Text;
			return WriteMethods.Exists(method => StringComparer.OrdinalIgnoreCase.Compare(nameString, method) == 0);
		}

		private bool RequiresReadWrite(InvocationExpressionSyntax invocation, string parameterName)
		{
			IEnumerable<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments.Where(arg => IsCallingParameter(arg.Expression, parameterName));
			if (!arguments.Any())
			{
				return false;
			}
			var needsReadWrite = false;
			ISymbol symbol = Context.SemanticModel.GetSymbolInfo(invocation).Symbol;
			foreach (ArgumentSyntax argument in arguments)
			{
				if (symbol != null)
				{
					needsReadWrite |= !IsReadOnly(symbol);
				}
			}
			return needsReadWrite;
		}

		private bool IsReadOnly(ISymbol symbol)
		{
			var symbolName = symbol.Name.ToLowerInvariant();
			return symbolName.Contains("readonly") || _comparer.Compare(symbolName, "System.Collections.Generic.IEnumerable") == 0;
		}
	}
}
