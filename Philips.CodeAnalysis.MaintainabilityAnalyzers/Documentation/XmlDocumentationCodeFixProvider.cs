﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocumentationCodeFixProvider)), Shared]
	public class XmlDocumentationCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Remove Summary";

		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.EmptyXmlComments), Helper.ToDiagnosticId(DiagnosticId.XmlDocumentationShouldAddValue));

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			if (root != null)
			{
				SyntaxNode node = root.FindNode(diagnosticSpan);

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
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode newRoot = root;
			foreach (SyntaxTrivia trivia in node.GetLeadingTrivia())
			{
				if (trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia)
				{
					newRoot = root.ReplaceTrivia(trivia, SyntaxTriviaList.Empty);

					// The formatting is slightly off after this. Rather than figure it out the hard way, just Format the whole document.
					newRoot = newRoot.WithAdditionalAnnotations(Formatter.Annotation);
				}
			}
			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}
