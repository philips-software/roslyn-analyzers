// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ProhibitDynamicKeywordAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Prohibit the ""dynamic"" Keyword";
		private const string MessageFormat = @"Do not use the ""dynamic"" keyword.  It it not compile time type safe.";
		private const string Description = @"The ""dynamic"" keyword is not checked for type safety at compile time.";

		public ProhibitDynamicKeywordAnalyzer()
			: base(DiagnosticId.DynamicKeywordProhibited, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
		}
		// TODO: Change to Analyze: MethodDeclaration, FieldDeclaration, VariableDeclaration, PropertyDeclaration
		// TODO: And reduce the number of items inspected by the SemanticModel
		
		private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
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
