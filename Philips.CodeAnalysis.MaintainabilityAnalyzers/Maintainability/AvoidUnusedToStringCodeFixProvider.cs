// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidUnusedToStringCodeFixProvider)), Shared]
	public class AvoidUnusedToStringCodeFixProvider : SingleDiagnosticCodeFixProvider<InvocationExpressionSyntax>
	{
		protected override string Title => "Remove unnecessary ToString() call";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidUnusedToString;

		protected override async Task<Document> ApplyFix(Document document, InvocationExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
			{
				return document;
			}

			ExpressionSyntax receiver = memberAccess.Expression;

			// Case 1: Standalone expression statement (expr.ToString();)
			if (node.Parent is ExpressionStatementSyntax expressionStatement)
			{
				// Only remove the entire statement when the receiver is known to be side-effect free:
				// local variable, parameter, or 'this'.
				if (IsSideEffectFree(receiver))
				{
					rootNode = rootNode.RemoveNode(expressionStatement, SyntaxRemoveOptions.KeepDirectives);
				}
				else
				{
					// Keep the receiver expression as a standalone statement to preserve side effects.
					ExpressionStatementSyntax newStatement = expressionStatement
						.WithExpression(receiver.WithoutTrivia())
						.WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
						.WithTrailingTrivia(expressionStatement.GetTrailingTrivia())
						.WithAdditionalAnnotations(Formatter.Annotation);
					rootNode = rootNode.ReplaceNode(expressionStatement, newStatement);
				}

				return document.WithSyntaxRoot(rootNode);
			}

			// Case 2: Assignment to discard (_ = expr.ToString()) - replace with just the expression
			SyntaxTriviaList trivia = node.GetLeadingTrivia();
			ExpressionSyntax newExpression = receiver.WithLeadingTrivia(trivia).WithAdditionalAnnotations(Formatter.Annotation);
			rootNode = rootNode.ReplaceNode(node, newExpression);
			return document.WithSyntaxRoot(rootNode);
		}

		private static bool IsSideEffectFree(ExpressionSyntax expression)
		{
			return expression is IdentifierNameSyntax or ThisExpressionSyntax;
		}
	}
}
