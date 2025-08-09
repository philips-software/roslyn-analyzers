// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidAssignmentInConditionCodeFixProvider)), Shared]
	public class AvoidAssignmentInConditionCodeFixProvider : SingleDiagnosticCodeFixProvider<ExpressionSyntax>
	{
		protected override string Title => "Extract assignment from condition";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidAssignmentInCondition;

		protected override ExpressionSyntax GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			// Find the condition that contains the assignment
			SyntaxNode node = root.FindNode(diagnosticSpan, false, true);

			// Look for if statement or ternary condition
			if (node is IfStatementSyntax ifStatement)
			{
				return ifStatement.Condition;
			}

			if (node is ConditionalExpressionSyntax ternary)
			{
				return ternary.Condition;
			}

			// If the node itself is the condition expression
			if (node is ExpressionSyntax expression)
			{
				// Verify this is actually inside an if statement or ternary
				IfStatementSyntax parentIf = expression.FirstAncestorOrSelf<IfStatementSyntax>();
				ConditionalExpressionSyntax parentTernary = expression.FirstAncestorOrSelf<ConditionalExpressionSyntax>();

				if (parentIf != null && parentIf.Condition.Contains(expression))
				{
					return parentIf.Condition;
				}

				if (parentTernary != null && parentTernary.Condition.Contains(expression))
				{
					return parentTernary.Condition;
				}

				// Handle the case where the expression itself is the condition
				return expression;
			}

			return null;
		}

		protected override async Task<Document> ApplyFix(Document document, ExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			ExpressionSyntax conditionExpression = node;

			// Handle the simple case where the condition itself is an assignment
			if (conditionExpression is AssignmentExpressionSyntax assignmentExpression)
			{
				return await HandleAssignmentExpression(document, rootNode, conditionExpression, assignmentExpression, cancellationToken).ConfigureAwait(false);
			}

			// Find the assignment expression within the condition (for more complex expressions)
			AssignmentExpressionSyntax nestedAssignment = conditionExpression.DescendantNodesAndSelf()
				.OfType<AssignmentExpressionSyntax>()
				.FirstOrDefault(a => a.IsKind(SyntaxKind.SimpleAssignmentExpression));

			if (nestedAssignment != null)
			{
				return await HandleAssignmentExpression(document, rootNode, conditionExpression, nestedAssignment, cancellationToken).ConfigureAwait(false);
			}

			return document; // No assignment found
		}

		private async Task<Document> HandleAssignmentExpression(Document document, SyntaxNode rootNode, ExpressionSyntax conditionExpression, AssignmentExpressionSyntax assignmentExpression, CancellationToken cancellationToken)
		{
			// Handle cases like: if (x = someExpression) or complex expressions containing assignment
			ExpressionSyntax leftSide = assignmentExpression.Left;

			// Create assignment statement
			ExpressionStatementSyntax assignmentStatement = SyntaxFactory.ExpressionStatement(assignmentExpression);

			// Use the left side as the new condition, or if the assignment is the entire condition, use just the left side
			ExpressionSyntax newCondition;
			if (conditionExpression == assignmentExpression)
			{
				// The entire condition is the assignment, so just use the left side with no trailing trivia
				newCondition = leftSide.WithoutTrailingTrivia();
			}
			else
			{
				// Replace the assignment within the larger condition expression
				newCondition = conditionExpression.ReplaceNode(assignmentExpression, leftSide.WithoutTrailingTrivia());
			}

			return await ReplaceConditionWithExtractedAssignment(document, rootNode, conditionExpression, assignmentStatement, newCondition, cancellationToken).ConfigureAwait(false);
		}

		private async Task<Document> ReplaceConditionWithExtractedAssignment(Document document, SyntaxNode rootNode, ExpressionSyntax conditionExpression, StatementSyntax extractedStatement, ExpressionSyntax newCondition, CancellationToken cancellationToken)
		{
			// Find the statement that contains the condition
			IfStatementSyntax containingStatement = conditionExpression.FirstAncestorOrSelf<IfStatementSyntax>();
			if (containingStatement == null)
			{
				// Handle ternary expression - this is more complex
				ConditionalExpressionSyntax ternary = conditionExpression.FirstAncestorOrSelf<ConditionalExpressionSyntax>();
				if (ternary != null)
				{
					// For ternary, we need to extract to a statement context
					StatementSyntax parentStatement = ternary.FirstAncestorOrSelf<StatementSyntax>();
					if (parentStatement != null)
					{
						return await HandleTernaryInStatement(document, rootNode, ternary, extractedStatement, newCondition, parentStatement, cancellationToken).ConfigureAwait(false);
					}
				}
				return document;
			}

			// Replace the condition in the if statement
			IfStatementSyntax newIfStatement = containingStatement.WithCondition(newCondition);

			// Move leading trivia from the if statement to the extracted statement
			SyntaxTriviaList leadingTrivia = containingStatement.GetLeadingTrivia();
			StatementSyntax formattedExtractedStatement = extractedStatement.WithLeadingTrivia(leadingTrivia);
			IfStatementSyntax newIfStatementWithTrivia = newIfStatement.WithoutLeadingTrivia();

			// Preserve some indentation on the if statement
			if (leadingTrivia.Count > 0)
			{
				newIfStatementWithTrivia = newIfStatementWithTrivia.WithLeadingTrivia(leadingTrivia[leadingTrivia.Count - 1]);
			}

			// If we're not already inside a block statement, we need to make it so
			if (containingStatement.Parent is StatementSyntax and not BlockSyntax)
			{
				BlockSyntax blockSyntax = SyntaxFactory.Block(formattedExtractedStatement, newIfStatementWithTrivia);
				rootNode = rootNode.ReplaceNode(containingStatement, blockSyntax);
			}
			else
			{
				// Replace the if statement with both the extracted statement and the new if statement
				SyntaxNode[] newNodes = { formattedExtractedStatement, newIfStatementWithTrivia };
				rootNode = rootNode.ReplaceNode(containingStatement, newNodes);
			}

			return document.WithSyntaxRoot(rootNode);
		}

		private Task<Document> HandleTernaryInStatement(Document document, SyntaxNode rootNode, ConditionalExpressionSyntax ternary, StatementSyntax extractedStatement, ExpressionSyntax newCondition, StatementSyntax parentStatement, CancellationToken _)
		{
			// Replace the ternary condition with the new condition
			ConditionalExpressionSyntax newTernary = ternary.WithCondition(newCondition);
			StatementSyntax newParentStatement = parentStatement.ReplaceNode(ternary, newTernary);

			// Move leading trivia
			SyntaxTriviaList leadingTrivia = parentStatement.GetLeadingTrivia();
			StatementSyntax formattedExtractedStatement = extractedStatement.WithLeadingTrivia(leadingTrivia);
			StatementSyntax newParentWithTrivia = newParentStatement.WithoutLeadingTrivia();

			if (leadingTrivia.Count > 0)
			{
				newParentWithTrivia = newParentWithTrivia.WithLeadingTrivia(leadingTrivia[leadingTrivia.Count - 1]);
			}

			// If we're not already inside a block statement, we need to make it so
			if (parentStatement.Parent is StatementSyntax and not BlockSyntax)
			{
				BlockSyntax blockSyntax = SyntaxFactory.Block(formattedExtractedStatement, newParentWithTrivia);
				rootNode = rootNode.ReplaceNode(parentStatement, blockSyntax);
			}
			else
			{
				SyntaxNode[] newNodes = { formattedExtractedStatement, newParentWithTrivia };
				rootNode = rootNode.ReplaceNode(parentStatement, newNodes);
			}

			return Task.FromResult(document.WithSyntaxRoot(rootNode));
		}
	}
}