// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Diagnostic for incorrect condition in backwards for-loops.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidIncorrectForLoopConditionAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = "Backwards for-loop boundary check should use >= 0 instead of > 0";
		private const string MessageFormat = "Use '>= 0' instead of '> 0' in backwards for-loop condition to include element at index 0";
		private const string Description = "When looping backwards from a collection count to 0, use '>= 0' to ensure the element at index 0 is processed";

		public AvoidIncorrectForLoopConditionAnalyzer()
			: base(DiagnosticId.AvoidIncorrectForLoopCondition, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeForStatement, SyntaxKind.ForStatement);
		}

		private void AnalyzeForStatement(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var forStatement = (ForStatementSyntax)context.Node;

			// Check if this is a backwards loop with problematic condition
			if (IsBackwardsLoopWithIncorrectCondition(forStatement))
			{
				Location location = forStatement.Condition.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location));
			}
		}

		private static bool IsBackwardsLoopWithIncorrectCondition(ForStatementSyntax forStatement)
		{
			// Must have a condition
			if (forStatement.Condition == null)
			{
				return false;
			}

			// Must have incrementors (decrementors in this case)
			if (forStatement.Incrementors.Count == 0)
			{
				return false;
			}

			// Check if any incrementor is decrementing
			var hasDecrement = false;
			var decrementedVariable = string.Empty;

			foreach (ExpressionSyntax incrementor in forStatement.Incrementors)
			{
				if (IsDecrementOperation(incrementor, out var varName))
				{
					hasDecrement = true;
					decrementedVariable = varName;
					break;
				}
			}

			if (!hasDecrement || string.IsNullOrEmpty(decrementedVariable))
			{
				return false;
			}

			// Check if condition is "variable > 0"
			return IsGreaterThanZeroCondition(forStatement.Condition, decrementedVariable);
		}

		private static bool IsDecrementOperation(ExpressionSyntax expression, out string variableName)
		{
			variableName = null;

			switch (expression)
			{
				// Handle i--, --i
				case PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostDecrementExpression):
					if (postfix.Operand is IdentifierNameSyntax postfixIdentifier)
					{
						variableName = postfixIdentifier.Identifier.ValueText;
						return true;
					}
					break;

				case PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreDecrementExpression):
					if (prefix.Operand is IdentifierNameSyntax prefixIdentifier)
					{
						variableName = prefixIdentifier.Identifier.ValueText;
						return true;
					}
					break;

				// Handle i -= 1
				case AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.SubtractAssignmentExpression):
					if (assignment.Left is IdentifierNameSyntax subtractIdentifier && IsLiteralOne(assignment.Right))
					{
						variableName = subtractIdentifier.Identifier.ValueText;
						return true;
					}
					break;

				// Handle i = i - 1
				case AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.SimpleAssignmentExpression):
					if (assignment.Left is IdentifierNameSyntax leftId &&
						assignment.Right is BinaryExpressionSyntax binary &&
						binary.IsKind(SyntaxKind.SubtractExpression) &&
						binary.Left is IdentifierNameSyntax rightId &&
						leftId.Identifier.ValueText == rightId.Identifier.ValueText &&
						IsLiteralOne(binary.Right))
					{
						variableName = leftId.Identifier.ValueText;
						return true;
					}
					break;
			}

			return false;
		}

		private static bool IsLiteralOne(ExpressionSyntax expression)
		{
			return expression is LiteralExpressionSyntax literal &&
				   literal.IsKind(SyntaxKind.NumericLiteralExpression) &&
				   literal.Token.ValueText == "1";
		}

		private static bool IsGreaterThanZeroCondition(ExpressionSyntax condition, string variableName)
		{
			if (condition is BinaryExpressionSyntax binary)
			{
				// Check for "variable > 0"
				if (binary.IsKind(SyntaxKind.GreaterThanExpression) &&
					binary.Left is IdentifierNameSyntax leftId &&
					leftId.Identifier.ValueText == variableName &&
					IsLiteralZero(binary.Right))
				{
					return true;
				}

				// Check for "0 < variable" (equivalent but less common)
				if (binary.IsKind(SyntaxKind.LessThanExpression) &&
					IsLiteralZero(binary.Left) &&
					binary.Right is IdentifierNameSyntax rightId &&
					rightId.Identifier.ValueText == variableName)
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsLiteralZero(ExpressionSyntax expression)
		{
			return expression is LiteralExpressionSyntax literal &&
				   literal.IsKind(SyntaxKind.NumericLiteralExpression) &&
				   literal.Token.ValueText == "0";
		}
	}
}
