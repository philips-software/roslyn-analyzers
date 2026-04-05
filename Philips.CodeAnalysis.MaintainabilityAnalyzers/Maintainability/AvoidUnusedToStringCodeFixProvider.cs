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

			// Case 1: Standalone expression statement (expr.ToString();) - remove the entire statement
			if (node.Parent is ExpressionStatementSyntax expressionStatement)
			{
				rootNode = rootNode.RemoveNode(expressionStatement, SyntaxRemoveOptions.KeepDirectives);
				return document.WithSyntaxRoot(rootNode);
			}

			// Case 2: Assignment to discard (_ = expr.ToString()) - replace with just the expression
			ExpressionSyntax expression = memberAccess.Expression;
			SyntaxTriviaList trivia = node.GetLeadingTrivia();
			ExpressionSyntax newExpression = expression.WithLeadingTrivia(trivia).WithAdditionalAnnotations(Formatter.Annotation);
			rootNode = rootNode.ReplaceNode(node, newExpression);
			return document.WithSyntaxRoot(rootNode);
		}
	}
}
