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
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssertAreEqualLiteralAnalyzer)), Shared]
	public class AssertAreEqualLiteralCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Refactor AreEqual(<literal true/false>, other) into IsTrue/IsFalse";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.AssertAreEqualLiteral)); }
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
				SyntaxNode syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
				if (syntaxNode != null)
				{
					InvocationExpressionSyntax invocationExpression = syntaxNode.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

					ArgumentSyntax arg = invocationExpression.ArgumentList.Arguments[0];

					Helper helper = new();
					if (!helper.IsLiteralTrueFalse(arg.Expression))
					{
						return;
					}

					// Register a code action that will invoke the fix.
					context.RegisterCodeFix(
						CodeAction.Create(
							title: Title,
							createChangedDocument: c => AssertAreEqualFix(context.Document, invocationExpression, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		private async Task<Document> AssertAreEqualFix(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			ArgumentSyntax literalExpected = invocationExpression.ArgumentList.Arguments[0];
			ArgumentSyntax actual = invocationExpression.ArgumentList.Arguments[1];

			ArgumentSyntax message = null;
			if (invocationExpression.ArgumentList.Arguments.Count > 2)
			{
				message = invocationExpression.ArgumentList.Arguments[2];
			}

			InvocationExpressionSyntax newInvocation = ConvertToInvocation(((MemberAccessExpressionSyntax)invocationExpression.Expression).Name, literalExpected.Expression, actual.Expression, message?.Expression);
			var trivia = invocationExpression.GetLeadingTrivia();
			var newInvocationWithTrivia = newInvocation.WithLeadingTrivia(trivia);
			root = root.ReplaceNode(invocationExpression, newInvocationWithTrivia);

			return document.WithSyntaxRoot(root);
		}

		private InvocationExpressionSyntax ConvertToInvocation(SimpleNameSyntax calledMethod, ExpressionSyntax literalExpected, ExpressionSyntax actual, ExpressionSyntax message)
		{
			bool isLiteralTrue = GetMethod(literalExpected);

			if (calledMethod.ToString() == StringConstants.AreNotEqualMethodName)
			{
				isLiteralTrue = !isLiteralTrue;
			}

			string method = isLiteralTrue ? "IsTrue" : "IsFalse";

			ArgumentListSyntax argumentListSyntax = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(actual) }));

			if (message != null)
			{
				argumentListSyntax = argumentListSyntax.AddArguments(SyntaxFactory.Argument(message));
			}

			return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseTypeName("Assert"), SyntaxFactory.Token(SyntaxKind.DotToken), (SimpleNameSyntax)SyntaxFactory.ParseName(method)), argumentListSyntax);
		}

		private bool GetMethod(ExpressionSyntax literalExpected)
		{
			if (literalExpected is LiteralExpressionSyntax literal)
			{
				return literal.Token.IsKind(SyntaxKind.TrueKeyword);
			}
			else if (literalExpected is PrefixUnaryExpressionSyntax prefixUnaryExpressionSyntax)
			{
				return !GetMethod(prefixUnaryExpressionSyntax.Operand);
			}

			throw new ArgumentException(@"A literal was expected.", nameof(literalExpected));
		}
	}
}
