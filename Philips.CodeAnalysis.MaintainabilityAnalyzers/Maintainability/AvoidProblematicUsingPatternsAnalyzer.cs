// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
				SymbolInfo symbolInfo = Context.SemanticModel.GetSymbolInfo(identifier);
				if (symbolInfo.Symbol is IFieldSymbol)
				{
					return true;
				}

				if (symbolInfo.Symbol is ILocalSymbol localSymbol)
				{
					// Check if this local symbol is declared as an out parameter
					if (IsOutParameterDeclaration(localSymbol))
					{
						return false; // Out parameters are safe to use
					}
					return true;
				}
			}

			return false;
		}

		private bool IsOutParameterDeclaration(ILocalSymbol localSymbol)
		{
			// Find the declaration syntax for this local symbol
			SyntaxReference declarationSyntax = localSymbol.DeclaringSyntaxReferences.FirstOrDefault();
			if (declarationSyntax == null)
			{
				return false;
			}

			// Check if the declaration is within an ArgumentSyntax with an out modifier
			ArgumentSyntax argumentSyntax = declarationSyntax.GetSyntax().Ancestors().OfType<ArgumentSyntax>().FirstOrDefault();
			return argumentSyntax?.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) == true;
		}
	}
}
