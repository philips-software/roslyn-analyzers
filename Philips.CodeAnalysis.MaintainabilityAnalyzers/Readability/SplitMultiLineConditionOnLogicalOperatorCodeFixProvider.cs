// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp,
		 Name = nameof(SplitMultiLineConditionOnLogicalOperatorCodeFixProvider)), Shared]
	public class SplitMultiLineConditionOnLogicalOperatorCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Split multiline conditions on logical operators";
		private static readonly SyntaxAnnotation annotation = new($"SplitCondition");

		public override ImmutableArray<string> FixableDiagnosticIds =>
			ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.SplitMultiLineConditionOnLogicalOperator));

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();

			var diagnosticSpan = diagnostic.Location.SourceSpan;

			if (root != null)
			{
				var node = root.FindNode(diagnosticSpan);
				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => ApplyCodeFix(context.Document, node, c),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> ApplyCodeFix(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (root == null)
			{
				return document;
			}

			// First remove the EOL from the violating token.
			var oldTrivia = node.GetTrailingTrivia();
			var index = oldTrivia.IndexOf(SyntaxKind.EndOfLineTrivia);
			if (index >= 0)
			{
				SyntaxTriviaList newTrivia = oldTrivia.RemoveAt(index);
				var replacementNode = node.WithTrailingTrivia(newTrivia)
					.WithAdditionalAnnotations(Formatter.Annotation, annotation);
				root = root.ReplaceNode(node, replacementNode);
			}

			// Next add EOL to the || or && token immediately following.
			var newNode = root.GetAnnotatedNodes(annotation).FirstOrDefault() ?? node;
			var logicalToken = newNode.GetLastToken().GetNextToken();
			if (logicalToken.Text is "||" or "&&")
			{
				var newLineTrivia = SyntaxFactory.CarriageReturnLineFeed;

				var newToken = logicalToken.WithLeadingTrivia().WithTrailingTrivia(newLineTrivia)
						.WithAdditionalAnnotations(Formatter.Annotation);
				root = root.ReplaceToken(logicalToken, newToken);
			}
			return document.WithSyntaxRoot(root);
		}
	}
}
