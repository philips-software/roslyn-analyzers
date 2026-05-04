// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;
using Document = Microsoft.CodeAnalysis.Document;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	/// <summary>
	/// Rewrites <c>Assert.AreEqual(expected.Count, actual.Count)</c> to
	/// <c>Assert.HasCount(expected.Count, actual)</c>.
	/// </summary>
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferAssertHasCountCodeFixProvider)), Shared]
	public class PreferAssertHasCountCodeFixProvider : SingleDiagnosticCodeFixProvider<InvocationExpressionSyntax>
	{
		private const string HasCountMethodName = "HasCount";

		protected override string Title => "Use Assert.HasCount";

		protected override DiagnosticId DiagnosticId => DiagnosticId.PreferAssertHasCount;

		protected override async Task<Document> ApplyFix(Document document, InvocationExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (root == null || node == null)
			{
				return document;
			}

			if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
			{
				return document;
			}

			ArgumentListSyntax argumentList = node.ArgumentList;
			if (argumentList == null || argumentList.Arguments.Count < 2)
			{
				return document;
			}

			if (argumentList.Arguments[1].Expression is not MemberAccessExpressionSyntax secondMemberAccess)
			{
				return document;
			}

			MemberAccessExpressionSyntax newMemberAccess = memberAccess.WithName(
				SyntaxFactory.IdentifierName(HasCountMethodName).WithTriviaFrom(memberAccess.Name));

			ArgumentSyntax firstArgument = argumentList.Arguments[0];
			ArgumentSyntax secondArgument = argumentList.Arguments[1];

			ExpressionSyntax strippedCollection = secondMemberAccess.Expression.WithTriviaFrom(secondMemberAccess);
			ArgumentSyntax newSecondArgument = secondArgument.WithExpression(strippedCollection);

			var newArguments = new List<ArgumentSyntax> { firstArgument, newSecondArgument };
			for (var i = 2; i < argumentList.Arguments.Count; i++)
			{
				newArguments.Add(argumentList.Arguments[i]);
			}

			ArgumentListSyntax newArgumentList = argumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments));

			InvocationExpressionSyntax newInvocation = node
				.WithExpression(newMemberAccess)
				.WithArgumentList(newArgumentList);

			SyntaxNode newRoot = root.ReplaceNode(node, newInvocation);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
