// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidMultipleLambdasOnSingleLineCodeFixProvider)), Shared]
	public class AvoidMultipleLambdasOnSingleLineCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Put every lambda on a separate line";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.AvoidMultipleLambdasOnSingleLine));

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
				SyntaxNode diagnosticNode = root.FindNode(diagnosticSpan);
				LambdaExpressionSyntax secondLambda = diagnosticNode.ChildNodes().OfType<LambdaExpressionSyntax>().FirstOrDefault();
				SyntaxNode parent = GetParentOnHigherLine(secondLambda);
				if (secondLambda is null || parent is null)
				{
					// Apparently, the code is different than what we expect.
					return;
				}
				LambdaExpressionSyntax firstLambdaOnLine = GetOtherLambdaOnSameLine(parent, secondLambda);
				// Nice location to break the line, after the closing parenthesis of the ArgumentList where the first lambda is part of.
				SyntaxNode parentOfFirst = firstLambdaOnLine?.Parent?.Parent;
				if (parentOfFirst is not null)
				{
					context.RegisterCodeFix(
						CodeAction.Create(
							title: Title,
							createChangedDocument: c => AddNewLineAfter(context.Document, parentOfFirst, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		private SyntaxNode GetParentOnHigherLine(SyntaxNode node)
		{
			var lineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line;
			return node
				.Ancestors()
				.FirstOrDefault(l => l.GetLocation().GetLineSpan().StartLinePosition.Line < lineNumber);
		}

		private LambdaExpressionSyntax GetOtherLambdaOnSameLine(SyntaxNode parent, LambdaExpressionSyntax second)
		{
			var lineNumber = second.GetLocation().GetLineSpan().StartLinePosition.Line;
			return parent
				.DescendantNodes()
				.OfType<LambdaExpressionSyntax>()
				.Where(k => !ReferenceEquals(k, second))
				.FirstOrDefault(l => l.GetLocation().GetLineSpan().StartLinePosition.Line == lineNumber);
		}

		private async Task<Document> AddNewLineAfter(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			SyntaxNode oldNode = node;
			SyntaxToken lastToken = oldNode.GetLastToken();
			// If the 2 lambdas are distinct statements, put the new line after the semicolon.
			if (lastToken.GetNextToken().IsKind(SyntaxKind.SemicolonToken))
			{
				lastToken = lastToken.GetNextToken();
				oldNode = lastToken.Parent;
			}
			SyntaxTriviaList newTrivia = lastToken.TrailingTrivia.Add(SyntaxFactory.EndOfLine(StringConstants.WindowsNewLine));

			SyntaxNode newNode = oldNode.WithTrailingTrivia(newTrivia).WithAdditionalAnnotations(Formatter.Annotation);
			root = root.ReplaceNode(oldNode, newNode);

			return document.WithSyntaxRoot(root);
		}
	}
}
