// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ProhibitDynamicKeywordAnalyzer : SingleDiagnosticAnalyzer<IdentifierNameSyntax, ProhibitDynamicKeywordSyntaxNodeAction>
	{
		private const string Title = @"Prohibit the ""dynamic"" Keyword";
		private const string MessageFormat = @"Do not use the ""dynamic"" keyword.  It it not compile time type safe.";
		private const string Description = @"The ""dynamic"" keyword is not checked for type safety at compile time and is prohibited.";

		public ProhibitDynamicKeywordAnalyzer()
			: base(DiagnosticId.DynamicKeywordProhibited, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class ProhibitDynamicKeywordSyntaxNodeAction : SyntaxNodeAction<IdentifierNameSyntax>
	{
		public override void Analyze()
		{
			if (IsIdentifierDynamicType())
			{
				var location = Node.GetLocation();
				ReportDiagnostic(location);
			}
		}

		private bool IsIdentifierDynamicType()
		{
			if (
				Node.Identifier.ValueText == "dynamic" &&
				!Node.Parent.IsKind(SyntaxKind.Argument) &&
				!Node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
			{
				return true;
			}

			if (Node.IsVar)
			{
				SymbolInfo symbol = Context.SemanticModel.GetSymbolInfo(Node);

				if (symbol.Symbol is IDynamicTypeSymbol)
				{
					return true;
				}
			}

			return false;
		}
	}
}
