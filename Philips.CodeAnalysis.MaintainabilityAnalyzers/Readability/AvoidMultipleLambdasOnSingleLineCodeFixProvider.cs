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
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidMultipleLambdasOnSingleLineCodeFixProvider)), Shared]
	public class AvoidMultipleLambdasOnSingleLineCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Put every linq statement on a separate line";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AvoidMultipleLambdasOnSingleLine));

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
				var diagnosticNode = root.FindNode(diagnosticSpan);
				var secondLambda = diagnosticNode.ChildNodes().OfType<LambdaExpressionSyntax>().FirstOrDefault();
				var parent = GetParentOnHigherLine(secondLambda);
				if (secondLambda is null || parent is null)
				{
					// Apparently, the code is different than what we expect.
					return;
				}
				var firstLambdaOnLine = GetOtherLambdaOnSameLine(parent, secondLambda);
				// Nice location to break the line, after the closing parenthesis of the ArgumentList where the first lambda is part of.
				var parentOfFirst = firstLambdaOnLine?.Parent?.Parent;
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
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			SyntaxToken lastToken = node.GetLastToken();
			if (lastToken.GetNextToken().IsKind(SyntaxKind.SemicolonToken))
			{
				lastToken = lastToken.GetNextToken();
				node = lastToken.Parent;
			}
			SyntaxTriviaList newTrivia = lastToken.TrailingTrivia.Add(SyntaxFactory.EndOfLine("\r\n"));

			root = root.ReplaceNode(node, node.WithTrailingTrivia(newTrivia)).WithAdditionalAnnotations(Formatter.Annotation);

			return document.WithSyntaxRoot(root);
		}
	}
}
