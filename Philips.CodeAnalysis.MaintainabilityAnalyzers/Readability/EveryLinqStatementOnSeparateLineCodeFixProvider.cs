// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EveryLinqStatementOnSeparateLineCodeFixProvider)), Shared]
	public class EveryLinqStatementOnSeparateLineCodeFixProvider : SingleDiagnosticCodeFixProvider<QueryClauseSyntax>
	{
		protected override string Title => "Put every linq statement on a separate line";

		protected override DiagnosticId DiagnosticId => DiagnosticId.EveryLinqStatementOnSeparateLine;

		protected override QueryClauseSyntax GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			return root.FindNode(diagnosticSpan) as QueryClauseSyntax;
		}

		protected override async Task<Document> ApplyFix(Document document, QueryClauseSyntax node, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			SyntaxToken lastToken = node.GetLastToken();
			SyntaxTriviaList newTrivia = lastToken.TrailingTrivia.Add(SyntaxFactory.EndOfLine(StringConstants.WindowsNewLine));

			QueryClauseSyntax clauseWithTrivia = node.WithTrailingTrivia(newTrivia);
			root = root.ReplaceNode(node, clauseWithTrivia).WithAdditionalAnnotations(Formatter.Annotation);

			return document.WithSyntaxRoot(root);
		}
	}
}
