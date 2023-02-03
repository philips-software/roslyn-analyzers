// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreventUnnecessaryRangeChecksCodeFixProvider)), Shared]
	public class PreventUnnecessaryRangeChecksCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Don't check a collections' Length or Count before iterating over it";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.PreventUncessaryRangeChecks));
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
				if (root.FindNode(diagnosticSpan) is not IfStatementSyntax node)
				{
					return;
				}

				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => RemoveIfStatement(context.Document, node, c),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> RemoveIfStatement(Document document, IfStatementSyntax node, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);

			var leadingTrivia = node.GetLeadingTrivia();
			var trailingTrivia = node.GetTrailingTrivia();

			SyntaxNode replaceNode = node.Statement;
			SyntaxNode ifBlock = node.Statement;

			if (ifBlock is BlockSyntax block)
			{
				StatementSyntax statement = block.Statements[0];

				trailingTrivia = trailingTrivia.AddRange(block.CloseBraceToken.LeadingTrivia);

				replaceNode = statement.WithLeadingTrivia(leadingTrivia);
			}
			else
			{
				replaceNode = replaceNode.WithLeadingTrivia(leadingTrivia);
			}
			var newLine = SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, Environment.NewLine);

			var trivia = trailingTrivia.Insert(0, newLine);
			replaceNode = replaceNode.WithTrailingTrivia(trivia);

			root = root.ReplaceNode(node, replaceNode).WithAdditionalAnnotations(Formatter.Annotation);

			return document.WithSyntaxRoot(root);
		}
	}
}
