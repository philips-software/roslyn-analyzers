// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp,
		 Name = nameof(SplitMultiLineConditionOnLogicalOperatorCodeFixProvider)), Shared]
	public class SplitMultiLineConditionOnLogicalOperatorCodeFixProvider : SingleDiagnosticCodeFixProvider<SyntaxNode>
	{
		private static readonly SyntaxAnnotation Annotation = new($"SplitCondition");

		protected override string Title => "Split multiline conditions on logical operators";

		protected override DiagnosticId DiagnosticId => DiagnosticId.SplitMultiLineConditionOnLogicalOperator;

		protected override SyntaxNode GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			return root.FindNode(diagnosticSpan);
		}

		protected override async Task<Document> ApplyFix(Document document, SyntaxNode node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (root == null)
			{
				return document;
			}

			// First remove the EOL from the violating token.
			SyntaxTriviaList oldTrivia = node.GetTrailingTrivia();
			var index = oldTrivia.IndexOf(SyntaxKind.EndOfLineTrivia);
			if (index >= 0)
			{
				SyntaxTriviaList newTrivia = oldTrivia.RemoveAt(index);
				SyntaxNode replacementNode = node.WithTrailingTrivia(newTrivia)
					.WithAdditionalAnnotations(Formatter.Annotation, Annotation);
				root = root.ReplaceNode(node, replacementNode);
			}

			// Next add EOL to the || or && token immediately following.
			SyntaxNode newNode = root.GetAnnotatedNodes(Annotation).FirstOrDefault() ?? node;
			SyntaxToken logicalToken = newNode.GetLastToken().GetNextToken();
			if (logicalToken.Text is "||" or "&&")
			{
				SyntaxTrivia newLineTrivia = SyntaxFactory.CarriageReturnLineFeed;

				SyntaxToken newToken = logicalToken.WithLeadingTrivia().WithTrailingTrivia(newLineTrivia)
						.WithAdditionalAnnotations(Formatter.Annotation);
				root = root.ReplaceToken(logicalToken, newToken);
			}
			return document.WithSyntaxRoot(root);
		}
	}
}
