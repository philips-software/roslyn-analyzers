// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

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

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidTaskResultCodeFixProvider)), Shared]
	public class AvoidTaskResultCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Use await";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AvoidTaskResult));
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
				SyntaxNode node = root.FindToken(diagnosticSpan.Start).Parent;
				if (node != null)
				{
					MemberAccessExpressionSyntax resultExpression = node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().First();

					context.RegisterCodeFix(
						CodeAction.Create(
							title: Title,
							createChangedDocument: c => ReplaceWithAwait(context.Document, resultExpression, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		private async Task<Document> ReplaceWithAwait(Document document, MemberAccessExpressionSyntax resultExpression, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			var newExpression = SyntaxFactory.AwaitExpression(resultExpression.Expression);
			rootNode = rootNode.ReplaceNode(resultExpression, newExpression.WithLeadingTrivia(newExpression.GetLeadingTrivia())).WithAdditionalAnnotations(Formatter.Annotation);
			return document.WithSyntaxRoot(rootNode);
		}
	}
}
