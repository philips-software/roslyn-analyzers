// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidThreadSleepCodeFixProvider)), Shared]
	public class AvoidThreadSleepCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Remove Thread.Sleep.";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AvoidThreadSleep)); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the method declaration identified by the diagnostic.
			if (root != null)
			{
				SyntaxNode node = root.FindToken(diagnosticSpan.Start).Parent;
				if (node != null)
				{
					InvocationExpressionSyntax invocationExpression = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

					// Register a code action that will invoke the fix.
					context.RegisterCodeFix(
						CodeAction.Create(
							title: Title,
							createChangedDocument: c => ThreadSleepFix(context.Document, invocationExpression.Parent, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		private async Task<Document> ThreadSleepFix(Document document, SyntaxNode syntaxNodeExpression, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode newRoot = rootNode.RemoveNode(syntaxNodeExpression, SyntaxRemoveOptions.KeepDirectives);

			Document newDocument = document.WithSyntaxRoot(newRoot);

			return newDocument;
		}
	}
}