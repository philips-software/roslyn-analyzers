// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CallExtensionMethodsAsInstanceMethodsAnalyzer)), Shared]
	public class CallExtensionMethodsAsInstanceMethodsCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.ExtensionMethodsCalledLikeInstanceMethods));

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			const string Title = "Call extension methods as if they were instance methods";

			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			if (root != null)
			{
				SyntaxToken token = root.FindToken(diagnosticSpan.Start);

				// Register a code action that will invoke the fix.
				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => AdjustCallingMechanism(context.Document, token),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> AdjustCallingMechanism(Document document, SyntaxToken token)
		{
			InvocationExpressionSyntax invocation = token.Parent.Ancestors().OfType<InvocationExpressionSyntax>().First();

			SyntaxNode root = await document.GetSyntaxRootAsync();

			SimpleNameSyntax name;
			if (invocation.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
			{
				name = memberAccessExpressionSyntax.Name;
			}
			else if (invocation.Expression is SimpleNameSyntax syntax)
			{
				name = syntax;
			}
			else
			{
				return document;
			}

			ExpressionSyntax thisObject = invocation.ArgumentList.Arguments[0].Expression;
			ArgumentListSyntax newArguments = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(invocation.ArgumentList.Arguments.Skip(1).ToArray()));

			MemberAccessExpressionSyntax newExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, thisObject, name.WithoutLeadingTrivia());

			InvocationExpressionSyntax newInvocation = SyntaxFactory.InvocationExpression(newExpression, newArguments);

			root = root.ReplaceNode(invocation, newInvocation.WithLeadingTrivia(invocation.GetLeadingTrivia()));

			return document.WithSyntaxRoot(root);
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}
	}
}
