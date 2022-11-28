// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	//	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyXmlCommentsCodeFixProvider)), Shared]
	public class XmlDocumentationCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Remove empty Summary comments";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.EmptyXmlComments)); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
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
						createChangedDocument: c => RemoveXmlComment(context.Document, node, c),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> RemoveXmlComment(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync();
			var newRoot = root;
			foreach (SyntaxTrivia trivia in node.GetLeadingTrivia())
			{
				if (trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia)
				{
					newRoot = root.ReplaceTrivia(trivia, SyntaxTriviaList.Empty);

					// The formatting is slightly off after this.  Rather than figure it out the hard way, just Format the whole document.
					newRoot = newRoot.WithAdditionalAnnotations(Formatter.Annotation);
				}
			}
			var newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}