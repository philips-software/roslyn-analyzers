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

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssertIsTrueParenthesisCodeFixProvider)), Shared]
	public class AssertIsTrueParenthesisCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Refactor IsTrue/IsFalse parentheses usage";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AssertIsTrueParenthesis)); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
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
							createChangedDocument: c => AssertIsTrueParenthesisFix(context.Document, invocationExpression, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		private async Task<Document> AssertIsTrueParenthesisFix(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
		{
			ArgumentSyntax arg = invocationExpression.ArgumentList.Arguments[0];

			ParenthesizedExpressionSyntax expression = (ParenthesizedExpressionSyntax)arg.Expression;

			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			root = root.ReplaceNode(expression, expression.Expression);

			return document.WithSyntaxRoot(root);
		}
	}
}
