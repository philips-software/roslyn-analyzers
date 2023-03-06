// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreventUnnecessaryRangeChecksCodeFixProvider)), Shared]
	public class PreventUnnecessaryRangeChecksCodeFixProvider : SingleDiagnosticCodeFixProvider<IfStatementSyntax>
	{
		protected override string Title => "Don't check a collections' Length or Count before iterating over it";

		protected override DiagnosticId DiagnosticId => DiagnosticId.PreventUnnecessaryRangeChecks;

		protected override IfStatementSyntax GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			return root.FindNode(diagnosticSpan) as IfStatementSyntax;
		}

		protected override async Task<Document> ApplyFix(Document document, IfStatementSyntax node, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			SyntaxTriviaList leadingTrivia = node.GetLeadingTrivia();
			SyntaxTriviaList trailingTrivia = node.GetTrailingTrivia();

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
			SyntaxTrivia newLine = SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, Environment.NewLine);

			SyntaxTriviaList trivia = trailingTrivia.Insert(0, newLine);
			replaceNode = replaceNode.WithTrailingTrivia(trivia);

			root = root.ReplaceNode(node, replaceNode).WithAdditionalAnnotations(Formatter.Annotation);

			return document.WithSyntaxRoot(root);
		}
	}
}
