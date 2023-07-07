// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public class LiteralHelper
	{
		public bool IsNull(ExpressionSyntax expression)
		{
			return expression is LiteralExpressionSyntax { Token.Text: "null" };
		}

		public bool IsLiteral(ExpressionSyntax expression, SemanticModel semanticModel)
		{
			if (expression is LiteralExpressionSyntax literal)
			{
				Optional<object> literalValue = semanticModel.GetConstantValue(literal);

				return literalValue.HasValue;
			}

			Optional<object> constant = semanticModel.GetConstantValue(expression);
			return constant.HasValue || IsConstantExpression(expression, semanticModel);
		}

		private bool IsConstantExpression(ExpressionSyntax expression, SemanticModel semanticModel)
		{
			// this assumes you've already checked for literals
			if (expression is MemberAccessExpressionSyntax)
			{
				// return true for member accesses that resolve to a constant e.g. SurveillanceConstants.TrendWidth
				Optional<object> constValue = semanticModel.GetConstantValue(expression);
				return constValue.HasValue;
			}
			else
			{
				if (expression is TypeOfExpressionSyntax typeOfExpression && typeOfExpression.Type is PredefinedTypeSyntax)
				{
					// return true for typeof(<static type>)
					return true;
				}
			}

			return false;
		}

		public bool IsTrueOrFalse(ExpressionSyntax expressionSyntax)
		{
			SyntaxKind kind = expressionSyntax.Kind();
			return kind switch
			{
				SyntaxKind.LogicalNotExpression => IsTrueOrFalse(((PrefixUnaryExpressionSyntax)expressionSyntax).Operand),//recurse.
				SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => true,//literal true/false
				_ => false,
			};
		}


	}
}
