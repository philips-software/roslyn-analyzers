// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssertAreEqualCodeFixProvider)), Shared]
	public class AssertAreEqualCodeFixProvider : SingleDiagnosticCodeFixProvider<InvocationExpressionSyntax>
	{
		private readonly Helper _helper;

		public AssertAreEqualCodeFixProvider()
			: this(new Helper())
		{ }
		public AssertAreEqualCodeFixProvider(Helper helper)
		{
			_helper = helper;
		}

		protected override string Title => "Refactor equality assertion";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AssertAreEqual;

		protected override async Task<Document> ApplyFix(Document document, InvocationExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			Document newDocument = document;
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);

			var isFirstArgumentNull = false;
			var isFirstArgumentConstant = false;
			ArgumentListSyntax argumentList = node.ArgumentList;
			if (argumentList.Arguments[0].Expression is LiteralExpressionSyntax arg0Literal)
			{
				Optional<object> literalValue = semanticModel.GetConstantValue(arg0Literal, cancellationToken);
				isFirstArgumentNull = literalValue.Value == null;
				isFirstArgumentConstant = true;
			}
			else
			{
				isFirstArgumentConstant = _helper.IsLiteral(argumentList.Arguments[0].Expression, semanticModel);
			}

			var isSecondArgumentNull = false;
			var isSecondArgumentConstant = false;
			if (argumentList.Arguments[1].Expression is LiteralExpressionSyntax arg1Literal)
			{
				Optional<object> literalValue = semanticModel.GetConstantValue(arg1Literal, cancellationToken);
				isSecondArgumentNull = literalValue.Value == null;
				isSecondArgumentConstant = true;
			}
			else
			{
				isSecondArgumentConstant = _helper.IsLiteral(argumentList.Arguments[1].Expression, semanticModel);
			}

			if (isFirstArgumentNull || isSecondArgumentNull)
			{
				return await ReplaceWithIsNull(document, argumentList, node, isFirstArgumentNull, cancellationToken);
			}
			else if (isSecondArgumentConstant && !isFirstArgumentConstant)
			{
				// swap the argument list
				ArgumentListSyntax newArgumentList = argumentList.ReplaceNodes(new SyntaxNode[] { argumentList.Arguments[0], argumentList.Arguments[1] },
					(original, other) => original == argumentList.Arguments[0] ? argumentList.Arguments[1] : argumentList.Arguments[0]);

				SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
				SyntaxNode newRoot = oldRoot.ReplaceNode(argumentList, newArgumentList);
				newDocument = document.WithSyntaxRoot(newRoot);
			}

			return newDocument;
		}

		private async Task<Document> ReplaceWithIsNull(Document document, ArgumentListSyntax argumentList, InvocationExpressionSyntax invocationExpressionSyntax, bool isFirstArgumentNull, CancellationToken cancellationToken)
		{
			// replace with IsNull or IsNotNull
			ArgumentSyntax argument;
			if (isFirstArgumentNull)
			{
				argument = argumentList.Arguments[1];
			}
			else
			{
				argument = argumentList.Arguments[0];
			}

			NameSyntax identifier;
			var memberAccess = (MemberAccessExpressionSyntax)invocationExpressionSyntax.Expression;
			var memberName = memberAccess.Name.ToString();
			if (memberName == StringConstants.AreEqualMethodName)
			{
				identifier = SyntaxFactory.ParseName(StringConstants.IsNullMethodName);
			}
			else
			{
				identifier = SyntaxFactory.ParseName(StringConstants.IsNotNullMethodName);
			}

			MemberAccessExpressionSyntax newMemberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, memberAccess.Expression, (SimpleNameSyntax)identifier);
			ArgumentListSyntax newArguments;

			if (argumentList.Arguments.Count == 2)
			{
				newArguments = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new ArgumentSyntax[] { argument }));
			}
			else
			{
				// make sure not to delete any custom error message
				newArguments = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new ArgumentSyntax[] { argument, argumentList.Arguments[2] }));
			}

			InvocationExpressionSyntax newInvocationExpression = SyntaxFactory.InvocationExpression(newMemberAccess, newArguments);
			SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode newRoot = oldRoot.ReplaceNode(invocationExpressionSyntax, newInvocationExpression);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
