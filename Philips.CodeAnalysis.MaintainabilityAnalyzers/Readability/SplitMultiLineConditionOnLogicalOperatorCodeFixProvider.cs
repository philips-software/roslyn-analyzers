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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp,
		 Name = nameof(SplitMultiLineConditionOnLogicalOperatorCodeFixProvider)), Shared]
	public class SplitMultiLineConditionOnLogicalOperatorCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Put every linq statement on a separate line";

		public override ImmutableArray<string> FixableDiagnosticIds =>
			ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.SplitMultiLineConditionOnLogicalOperator));

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
			var annotation = new SyntaxAnnotation("SplitCondition");

			// First remove the EOL from the violating token.
			var oldTrivia = node.GetTrailingTrivia();
			var index = oldTrivia.IndexOf(SyntaxKind.EndOfLineTrivia);
			if (index >= 0 && root != null)
			{
				SyntaxTriviaList newTrivia = oldTrivia.RemoveAt(index);
				var newNode = node.WithTrailingTrivia(newTrivia)
					.WithAdditionalAnnotations(Formatter.Annotation, annotation);
				root = root.ReplaceNode(node, newNode);
			}
			
			// Next add EOL to the || or && token immediately following.
			node = root.GetAnnotatedNodes(annotation).FirstOrDefault() ?? node;
			var logicalToken = node.GetLastToken().GetNextToken();
			var newLineTrivia = SyntaxFactory.CarriageReturnLineFeed;
			
			root = root.ReplaceToken(logicalToken, logicalToken.WithLeadingTrivia().WithTrailingTrivia(newLineTrivia).WithAdditionalAnnotations(Formatter.Annotation));

			return document.WithSyntaxRoot(root);
		}
	}
}
