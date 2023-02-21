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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EveryLinqStatementOnSeparateLineCodeFixProvider)), Shared]
	public class EveryLinqStatementOnSeparateLineCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Put every linq statement on a separate line";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.EveryLinqStatementOnSeparateLine));
		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();

			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			if (root != null)
			{
				if (root.FindNode(diagnosticSpan) is not QueryClauseSyntax clause)
				{
					return;
				}

				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => AddNewLineAfter(context.Document, clause, c),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> AddNewLineAfter(Document document, QueryClauseSyntax clause, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			SyntaxToken lastToken = clause.GetLastToken();
			SyntaxTriviaList newTrivia = lastToken.TrailingTrivia.Add(SyntaxFactory.EndOfLine(StringConstants.WindowsNewLine));

			QueryClauseSyntax clauseWithTrivia = clause.WithTrailingTrivia(newTrivia);
			root = root.ReplaceNode(clause, clauseWithTrivia).WithAdditionalAnnotations(Formatter.Annotation);

			return document.WithSyntaxRoot(root);
		}
	}
}
