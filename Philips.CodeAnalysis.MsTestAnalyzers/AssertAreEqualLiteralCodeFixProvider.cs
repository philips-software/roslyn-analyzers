// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssertAreEqualLiteralAnalyzer)), Shared]
	public class AssertAreEqualLiteralCodeFixProvider : SingleDiagnosticCodeFixProvider<InvocationExpressionSyntax>
	{
		protected override string Title => "Refactor AreEqual(<literal true/false>, other) into IsTrue/IsFalse";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AssertAreEqualLiteral;

		protected override async Task<Document> ApplyFix(Document document, InvocationExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			ArgumentSyntax literalExpected = node.ArgumentList.Arguments[0];
			ArgumentSyntax actual = node.ArgumentList.Arguments[1];

			ArgumentSyntax message = null;
			if (node.ArgumentList.Arguments.Count > 2)
			{
				message = node.ArgumentList.Arguments[2];
			}

			InvocationExpressionSyntax newInvocation = ConvertToInvocation(((MemberAccessExpressionSyntax)node.Expression).Name, literalExpected.Expression, actual.Expression, message?.Expression);
			SyntaxTriviaList trivia = node.GetLeadingTrivia();
			InvocationExpressionSyntax newInvocationWithTrivia = newInvocation.WithLeadingTrivia(trivia);
			root = root.ReplaceNode(node, newInvocationWithTrivia);

			return document.WithSyntaxRoot(root);
		}

		private InvocationExpressionSyntax ConvertToInvocation(SimpleNameSyntax calledMethod, ExpressionSyntax literalExpected, ExpressionSyntax actual, ExpressionSyntax message)
		{
			var isLiteralTrue = GetMethod(literalExpected);

			if (calledMethod.ToString() == StringConstants.AreNotEqualMethodName)
			{
				isLiteralTrue = !isLiteralTrue;
			}

			var method = isLiteralTrue ? StringConstants.IsTrue : StringConstants.IsFalse;

			ArgumentListSyntax argumentListSyntax = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(actual) }));

			if (message != null)
			{
				argumentListSyntax = argumentListSyntax.AddArguments(SyntaxFactory.Argument(message));
			}

			return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseTypeName(StringConstants.Assert), SyntaxFactory.Token(SyntaxKind.DotToken), (SimpleNameSyntax)SyntaxFactory.ParseName(method)), argumentListSyntax);
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
