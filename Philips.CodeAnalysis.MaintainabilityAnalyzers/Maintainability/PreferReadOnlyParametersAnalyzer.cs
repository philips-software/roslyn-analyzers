// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
		private static readonly List<string> ReadWriteCollections = new() { "System.Array", "System.Collections.Generic.List", "System.Span" };
		private readonly NamespaceIgnoringComparer _comparer = new();

		public override void Analyze()
		{
			var parameters = Node.ParameterList.Parameters;
			if (!parameters.Any())
			{
				return;
			}

			var aliases = Helper.GetUsingAliases(Node);
			var collections = parameters.Where(p => IsReadWriteCollection(p.Type, aliases));
			if (!collections.Any())
			{
				return;
			}
			var accesses = Node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();
			var setters = Node.DescendantNodes()
				.OfType<AssignmentExpressionSyntax>()
				.Select(ass => ass.Left as ElementAccessExpressionSyntax).ToList();
			var invocations = Node.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
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
					var location = collectionParameter.Type.GetLocation();
					ReportDiagnostic(location, collectionParameter.Identifier.Text);
				}
			}
		}

		private bool IsReadWriteCollection(TypeSyntax type, IReadOnlyDictionary<string, string> aliases)
		{
			if (type == null)
			{
				return false;
			}
			var fullName = type.GetFullName(aliases);
			return ReadWriteCollections.Any(col => _comparer.Compare(fullName, col) == 0);
		}

		private bool IsCallingParameter(ExpressionSyntax expression, string name)
		{
			return expression is IdentifierNameSyntax identifierName && _comparer.Compare(identifierName.Identifier.Text, name) == 0;
		}

		private static bool IsModifyingMember(SimpleNameSyntax name)
		{
			return name.Identifier.Text is "Add" or "AddRange" or "Clear" or "Insert" or "InsertRange" or "Remove" or "RemoveAt" or "RemoveRange" or "Reverse" or "Sort";
		}

		private bool RequiresReadWrite(InvocationExpressionSyntax invocation, string parameterName)
		{
			var arguments = invocation.ArgumentList.Arguments.Where(arg => IsCallingParameter(arg.Expression, parameterName));
			if (!arguments.Any())
			{
				return false;
			}
			bool needsReadWrite = false;
			var symbol = Context.SemanticModel.GetSymbolInfo(invocation).Symbol;
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
			return symbol.Name.ToLowerInvariant().Contains("readonly");
		}
	}
}
