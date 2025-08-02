// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidProblematicUsingPatternsAnalyzer : SingleDiagnosticAnalyzer<UsingStatementSyntax, AvoidProblematicUsingPatternsSyntaxNodeAction>
	{
		private const string Title = @"Avoid problematic using statement patterns";
		public const string MessageFormat = @"Avoid using statement that may lead to double disposal.";
		private const string Description = @"Avoid using statements with fields or variables.";

		public AvoidProblematicUsingPatternsAnalyzer()
			: base(DiagnosticId.AvoidProblematicUsingPatterns, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}
	public class AvoidProblematicUsingPatternsSyntaxNodeAction : SyntaxNodeAction<UsingStatementSyntax>
	{
		public override void Analyze()
		{
			if (Node.Declaration != null)
			{
				AnalyzeUsingDeclaration(Node.Declaration);
			}
			else if (Node.Expression != null)
			{
				AnalyzeUsingExpression(Node.Expression);
			}
		}

		private void AnalyzeUsingDeclaration(VariableDeclarationSyntax declaration)
		{
			foreach (VariableDeclaratorSyntax variable in declaration.Variables)
			{
				if (variable.Initializer?.Value != null)
				{
					ExpressionSyntax initializerValue = variable.Initializer.Value;
					if (IsProblematicExpression(initializerValue))
					{
						Location location = variable.GetLocation();
						ReportDiagnostic(location);
					}
				}
			}
		}

		private void AnalyzeUsingExpression(ExpressionSyntax expression)
		{
			if (IsProblematicExpression(expression))
			{
				Location location = expression.GetLocation();
				ReportDiagnostic(location);
			}
		}

		private bool IsProblematicExpression(ExpressionSyntax expression)
		{
			if (expression is MemberAccessExpressionSyntax)
			{
				return true;
			}

			if (expression is IdentifierNameSyntax identifier)
			{
				var name = identifier.Identifier.ValueText;
				if (name.StartsWith("_"))
				{
					return true;
				}

				SymbolInfo symbolInfo = Context.SemanticModel.GetSymbolInfo(identifier);
				if (symbolInfo.Symbol is IFieldSymbol)
				{
					return true;
				}

				if (symbolInfo.Symbol is ILocalSymbol)
				{
					return true;
				}
			}

			return false;
		}
	}
}
