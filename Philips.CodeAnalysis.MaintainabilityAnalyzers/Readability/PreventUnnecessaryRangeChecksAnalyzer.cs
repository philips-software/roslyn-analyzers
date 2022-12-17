// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreventUnnecessaryRangeChecksAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Unnecessary Range Checks";
		public const string MessageFormat = @"Do not check the length of a list/array before iterating over it";
		private const string Description = @"";
		private const string Category = Categories.Readability;

		public DiagnosticDescriptor Rule { get; } = new(Helper.ToDiagnosticId(DiagnosticIds.PreventUncessaryRangeChecks), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public PreventUnnecessaryRangeChecksAnalyzer()
		{ }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
		}

		private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
		{
			IfStatementSyntax ifStatementSyntax = (IfStatementSyntax)context.Node;

			if (ifStatementSyntax.Else != null)
			{
				return;
			}

			if (ifStatementSyntax.Condition == null)
			{
				return;
			}

			if (!TryFindForeach(ifStatementSyntax, out ForEachStatementSyntax forEachStatementSyntax))
			{
				return;
			}

			if (!IsCountGreaterThanZero(ifStatementSyntax.Condition, forEachStatementSyntax.Expression, context.SemanticModel))
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, ifStatementSyntax.IfKeyword.GetLocation()));
		}

		private bool TryFindForeach(IfStatementSyntax ifStatementSyntax, out ForEachStatementSyntax forEachStatementSyntax)
		{
			forEachStatementSyntax = null;

			if (ifStatementSyntax.Statement is not BlockSyntax block)
			{
				forEachStatementSyntax = ifStatementSyntax.Statement as ForEachStatementSyntax;

				return forEachStatementSyntax != null;
			}

			if (block.Statements.Count != 1)
			{
				return false;
			}

			forEachStatementSyntax = block.Statements[0] as ForEachStatementSyntax;

			if (forEachStatementSyntax == null)
			{
				return false;
			}

			return true;
		}

		private bool IsCountGreaterThanZero(ExpressionSyntax condition, ExpressionSyntax foreachExpression, SemanticModel semanticModel)
		{
			if (condition is ParenthesizedExpressionSyntax parenthesized)
			{
				return IsCountGreaterThanZero(parenthesized.Expression, foreachExpression, semanticModel);
			}

			if (condition is BinaryExpressionSyntax binaryExpressionSyntax)
			{
				return IsCountGreaterThanZero(binaryExpressionSyntax, foreachExpression, semanticModel);
			}

			return false;
		}
		private bool IsCountGreaterThanZero(BinaryExpressionSyntax condition, ExpressionSyntax foreachExpression, SemanticModel semanticModel)
		{
			switch (condition.OperatorToken.Kind())
			{
				case SyntaxKind.GreaterThanToken:
				case SyntaxKind.ExclamationEqualsToken:
					break;
				default:
					return false;
			}

			if (condition.Right is not LiteralExpressionSyntax literal)
			{
				return false;
			}

			if (literal.Token.Kind() != SyntaxKind.NumericLiteralToken)
			{
				return false;
			}

			if (literal.Token.ValueText != "0")
			{
				return false;
			}

			if (!TryGetIdentifiers(condition.Left, out ExpressionSyntax ifExpression, out IdentifierNameSyntax method))
			{
				return false;
			}

			switch (method.Identifier.Text)
			{
				case "Length":
				case "Count":
				case "Count()":
					break;
				default:
					return false;
			}

			if (foreachExpression is IdentifierNameSyntax identifier && ifExpression is IdentifierNameSyntax ifIdentifier)
			{
				if (ifIdentifier.Identifier.Text != identifier.Identifier.Text)
				{
					return false;
				}

				return true;
			}

			if (foreachExpression is MemberAccessExpressionSyntax memberAccess && ifExpression is MemberAccessExpressionSyntax ifMemberAccess)
			{
				static bool AreEqual(SyntaxNode left, SyntaxNode right, SemanticModel model)
				{
					var leftSymbol = model.GetSymbolInfo(left);
					if (leftSymbol.Symbol is null)
					{
						return false;
					}

					var rightSymbol = model.GetSymbolInfo(right);
					if (rightSymbol.Symbol is null)
					{
						return false;
					}

					if (!SymbolEqualityComparer.Default.Equals(leftSymbol.Symbol, rightSymbol.Symbol))
					{
						return false;
					}

					return true;
				}

				var foreachMemberAccessNodes = memberAccess.DescendantNodesAndSelf().ToList();
				var ifMemberAccessNodes = ifMemberAccess.DescendantNodesAndSelf().ToList();

				if (foreachMemberAccessNodes.Count != ifMemberAccessNodes.Count)
				{
					return false;
				}

				for (int i = 0; i < foreachMemberAccessNodes.Count; i++)
				{
					if (!AreEqual(foreachMemberAccessNodes[i], ifMemberAccessNodes[i], semanticModel))
					{
						return false;
					}
				}

				return true;
			}

			return false;
		}

		private bool TryGetIdentifiers(ExpressionSyntax expression, out ExpressionSyntax ifIdentifier, out IdentifierNameSyntax method)
		{
			ifIdentifier = null;
			method = null;

			if (expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
			{
				return TryGetIdentifiers(memberAccessExpressionSyntax, out ifIdentifier, out method);
			}

			if (expression is InvocationExpressionSyntax invocationExpression)
			{
				return TryGetIdentifiers(invocationExpression.Expression, out ifIdentifier, out method);
			}

			return false;
		}

		private bool TryGetIdentifiers(MemberAccessExpressionSyntax expression, out ExpressionSyntax ifIdentifier, out IdentifierNameSyntax method)
		{
			ifIdentifier = null;
			method = expression.Name as IdentifierNameSyntax;

			if (method == null)
			{
				return false;
			}

			ifIdentifier = expression.Expression as IdentifierNameSyntax;
			if (ifIdentifier != null)
			{
				return true;
			}

			ifIdentifier = expression.Expression as MemberAccessExpressionSyntax;
			if (ifIdentifier != null)
			{
				return true;
			}

			return false;
		}
	}
}
