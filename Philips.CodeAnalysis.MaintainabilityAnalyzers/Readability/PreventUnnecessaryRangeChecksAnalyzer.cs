// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreventUnnecessaryRangeChecksAnalyzer : SingleDiagnosticAnalyzer<IfStatementSyntax, PreventUnnecessaryRangeChecksSyntaxNodeAction>
	{
		private const string Title = @"Unnecessary Range Checks";
		public const string MessageFormat = @"Do not check the length of a list/array before iterating over it";
		private const string Description = MessageFormat;

		public PreventUnnecessaryRangeChecksAnalyzer()
			: base(DiagnosticId.PreventUncessaryRangeChecks, Title, MessageFormat, Description, Categories.Readability)
		{ }
	}

	public class PreventUnnecessaryRangeChecksSyntaxNodeAction : SyntaxNodeAction<IfStatementSyntax>
	{
		public override void Analyze()
		{
			if (Node.Else != null)
			{
				return;
			}

			if (Node.Condition == null)
			{
				return;
			}

			if (!TryFindForeach(out ForEachStatementSyntax forEachStatementSyntax))
			{
				return;
			}

			if (!IsCountGreaterThanZero(Node.Condition, forEachStatementSyntax.Expression, Context.SemanticModel))
			{
				return;
			}

			Location location = Node.IfKeyword.GetLocation();
			ReportDiagnostic(location);
		}

		private bool TryFindForeach(out ForEachStatementSyntax forEachStatementSyntax)
		{
			forEachStatementSyntax = null;

			if (Node.Statement is not BlockSyntax block)
			{
				forEachStatementSyntax = Node.Statement as ForEachStatementSyntax;

				return forEachStatementSyntax != null;
			}

			if (block.Statements.Count != 1)
			{
				return false;
			}

			forEachStatementSyntax = block.Statements[0] as ForEachStatementSyntax;

			return forEachStatementSyntax != null;
		}

		private bool IsCountGreaterThanZero(ExpressionSyntax condition, ExpressionSyntax foreachExpression, SemanticModel semanticModel)
		{
			bool isGreaterThanZero;
			if (condition is ParenthesizedExpressionSyntax parenthesized)
			{
				isGreaterThanZero = IsCountGreaterThanZero(parenthesized.Expression, foreachExpression, semanticModel);
			}
			else
			{
				isGreaterThanZero = condition is BinaryExpressionSyntax binaryExpressionSyntax &&
						 IsCountGreaterThanZero(binaryExpressionSyntax, foreachExpression, semanticModel);
			}

			return isGreaterThanZero;
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
				return ifIdentifier.Identifier.Text == identifier.Identifier.Text;
			}

			if (foreachExpression is not MemberAccessExpressionSyntax memberAccess || ifExpression is not MemberAccessExpressionSyntax ifMemberAccess)
			{
				return false;
			}

			var foreachMemberAccessNodes = memberAccess.DescendantNodesAndSelf().ToList();
			var ifMemberAccessNodes = ifMemberAccess.DescendantNodesAndSelf().ToList();

			if (foreachMemberAccessNodes.Count != ifMemberAccessNodes.Count)
			{
				return false;
			}

			static bool AreEqual(SyntaxNode left, SyntaxNode right, SemanticModel model)
			{
				SymbolInfo leftSymbol = model.GetSymbolInfo(left);
				if (leftSymbol.Symbol is null)
				{
					return false;
				}

				SymbolInfo rightSymbol = model.GetSymbolInfo(right);
				return rightSymbol.Symbol is not null && SymbolEqualityComparer.Default.Equals(leftSymbol.Symbol, rightSymbol.Symbol);
			}

			for (var i = 0; i < foreachMemberAccessNodes.Count; i++)
			{
				if (!AreEqual(foreachMemberAccessNodes[i], ifMemberAccessNodes[i], semanticModel))
				{
					return false;
				}
			}

			return true;
		}

		private bool TryGetIdentifiers(ExpressionSyntax expression, out ExpressionSyntax ifIdentifier, out IdentifierNameSyntax method)
		{
			ifIdentifier = null;
			method = null;

			return expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax ?
				TryGetIdentifiers(memberAccessExpressionSyntax, out ifIdentifier, out method) :
				expression is InvocationExpressionSyntax invocationExpression &&
				TryGetIdentifiers(invocationExpression.Expression, out ifIdentifier, out method);
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
			return ifIdentifier != null;
		}
	}
}
