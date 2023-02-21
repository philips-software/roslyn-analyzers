﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocumentThrownExceptionsCodeFixProvider)), Shared]
	public class DocumentThrownExceptionsCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Document thrown exceptions";

		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.DocumentThrownExceptions), Helper.ToDiagnosticId(DiagnosticId.DocumentUnhandledExceptions));

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			Diagnostic diagnostic = context.Diagnostics.First();
			Microsoft.CodeAnalysis.Text.TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
			if (!diagnostic.Properties.TryGetValue(StringConstants.ThrownExceptionPropertyKey, out string missingExceptionTypeName))
			{
				return;
			}

			if (root != null)
			{
				SyntaxNode diagnsticNode = root.FindNode(diagnosticSpan);
				SyntaxNode node = DocumentationHelper.FindAncestorThatCanHaveDocumentation(diagnsticNode);

				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => AddExceptionComment(context.Document, node, missingExceptionTypeName, c),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> AddExceptionComment(Document document, SyntaxNode node, string exceptionTypeName, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode newRoot = root;

			DocumentationHelper docHelper = new(node);
			string[] parts = exceptionTypeName.Split(',');
			foreach (string part in parts)
			{
				docHelper.AddException(part);
			}
			Microsoft.CodeAnalysis.CSharp.Syntax.DocumentationCommentTriviaSyntax newComment = docHelper.CreateDocumentation();

			if (docHelper.ExistingDocumentation != null)
			{
				newComment = newComment.WithAdditionalAnnotations(Formatter.Annotation);
				newRoot = root.ReplaceNode(docHelper.ExistingDocumentation, newComment);
			}

			return document.WithSyntaxRoot(newRoot);
		}
	}
}
