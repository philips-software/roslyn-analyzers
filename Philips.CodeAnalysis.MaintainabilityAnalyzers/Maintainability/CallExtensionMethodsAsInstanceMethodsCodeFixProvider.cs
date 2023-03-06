// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CallExtensionMethodsAsInstanceMethodsAnalyzer)), Shared]
	public class CallExtensionMethodsAsInstanceMethodsCodeFixProvider : SingleDiagnosticCodeFixProvider<InvocationExpressionSyntax>
	{
		protected override string Title => "Call extension methods as if they were instance methods";

		protected override DiagnosticId DiagnosticId => DiagnosticId.ExtensionMethodsCalledLikeInstanceMethods;

		protected override async Task<Document> ApplyFix(Document document, InvocationExpressionSyntax node, CancellationToken cancellationToken)
		{
			InvocationExpressionSyntax invocation = node;

			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

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

			name = name.WithoutLeadingTrivia();
			MemberAccessExpressionSyntax newExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, thisObject, name);

			InvocationExpressionSyntax newInvocation = SyntaxFactory.InvocationExpression(newExpression, newArguments);

			SyntaxTriviaList trivia = invocation.GetLeadingTrivia();
			InvocationExpressionSyntax newInvocationWithLeadingTrivia = newInvocation.WithLeadingTrivia(trivia);
			root = root.ReplaceNode(invocation, newInvocationWithLeadingTrivia);

			return document.WithSyntaxRoot(root);
		}
	}
}
