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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssertIsTrueCodeFixProvider)), Shared]
	public class AssertIsTrueCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Refactor IsTrue/IsFalse";

		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.AssertIsEqual));

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
				SyntaxNode syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
				if (syntaxNode != null)
				{
					InvocationExpressionSyntax invocationExpression = syntaxNode.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

					// Register a code action that will invoke the fix.
					context.RegisterCodeFix(
						CodeAction.Create(
							title: Title,
							createChangedDocument: c => AssertIsTrueFix(context.Document, invocationExpression, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		private async Task<Document> AssertIsTrueFix(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
		{
			string calledMethod = ((MemberAccessExpressionSyntax)invocationExpression.Expression).Name.Identifier.Text;

			bool isIsTrue = calledMethod == StringConstants.IsTrue;

			ArgumentSyntax arg = invocationExpression.ArgumentList.Arguments[0];

			return await HandleArgument(document, arg.Expression, invocationExpression, isIsTrue, cancellationToken);
		}

		private async Task<Document> HandleArgument(Document document, ExpressionSyntax arg, InvocationExpressionSyntax invocationExpression, bool isIsTrue, CancellationToken cancellationToken)
		{
			//the following should match AssertIsTrueAnalyzer::CheckForEquals

			SyntaxKind kind = arg.Kind();
			switch (kind)
			{
				case SyntaxKind.LogicalAndExpression:
					if (!isIsTrue)
					{
						break;
					}

					return await ReplaceBinaryAnd(document, invocationExpression, isIsTrue, cancellationToken);
				case SyntaxKind.EqualsExpression:
				case SyntaxKind.NotEqualsExpression:
					//some type of ==, !=
					return await ReplaceEqualsEquals(document, invocationExpression, isIsTrue, kind, cancellationToken);
				case SyntaxKind.InvocationExpression:
					//they called a function.  The analyzer has already decided it was of the form x.Equals(y)
					return await ReplaceEqualsFunctionCall(document, invocationExpression, isIsTrue, cancellationToken);
			}

			return document;
		}

		private async Task<Document> ReplaceBinaryAnd(Document document, InvocationExpressionSyntax invocationExpression, bool isIsTrue, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			ArgumentListSyntax arguments = invocationExpression.ArgumentList;

			var expr = (BinaryExpressionSyntax)arguments.Arguments[0].Expression;

			ExpressionSyntax left = expr.Left;
			ExpressionSyntax right = expr.Right;

			ArgumentSyntax[] additionalArguments = arguments.Arguments.Skip(1).ToArray();

			StatementSyntax leftAssert = CreateAssert(left, isIsTrue, additionalArguments);
			StatementSyntax rightAssert = CreateAssert(right, isIsTrue, additionalArguments);

			SyntaxNode node = invocationExpression;
			BlockSyntax parentBlock = null;
			while (parentBlock == null && node != null)
			{
				parentBlock = node.Parent as BlockSyntax;
				node = node.Parent;
			}

			// Should not happen
			if (parentBlock == null)
			{
				return document;
			}

			int statementIndex = parentBlock.Statements.IndexOf(x => x.Contains(invocationExpression));

			StatementSyntax statement = parentBlock.Statements[statementIndex];

			SyntaxTriviaList leadingTrivia = statement.GetLeadingTrivia();
			SyntaxTriviaList trailingTrivia = statement.GetTrailingTrivia();

			StatementSyntax rightAssertWithTrivia = rightAssert.WithLeadingTrivia().WithTrailingTrivia(trailingTrivia);
			SyntaxList<StatementSyntax> statements = parentBlock.Statements.Insert(statementIndex, rightAssertWithTrivia);
			StatementSyntax leftAssertWithTrivia = leftAssert.WithLeadingTrivia(leadingTrivia);
			statements = statements.Insert(statementIndex, leftAssertWithTrivia);
			statements = statements.RemoveAt(statementIndex + 2);

			BlockSyntax newBlock = parentBlock.WithStatements(statements);

			root = root.ReplaceNode(parentBlock, newBlock);

			return document.WithSyntaxRoot(root);
		}

		private StatementSyntax CreateAssert(ExpressionSyntax test, bool isIsTrue, ArgumentSyntax[] additionalArguments)
		{
			SeparatedSyntaxList<ArgumentSyntax> newArguments = new();

			newArguments = newArguments.Add(SyntaxFactory.Argument(test));

			newArguments = newArguments.AddRange(additionalArguments);

			MemberAccessExpressionSyntax memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.IdentifierName(StringConstants.Assert), SyntaxFactory.IdentifierName(isIsTrue ? StringConstants.IsTrue : StringConstants.IsFalse)
					).WithOperatorToken(SyntaxFactory.Token(SyntaxKind.DotToken));
			InvocationExpressionSyntax invocationExpression = SyntaxFactory.InvocationExpression(memberAccessExpression).WithArgumentList(SyntaxFactory.ArgumentList(arguments: newArguments));
			return SyntaxFactory.ExpressionStatement(invocationExpression);
		}

		private async Task<Document> ReplaceEqualsFunctionCall(Document document, InvocationExpressionSyntax invocationExpression, bool isIsTrue, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			ArgumentListSyntax newArguments = DecomposeEqualsFunction(invocationExpression.ArgumentList, out bool isNotEquals);

			string functionName = DetermineFunction(isIsTrue, isNotEquals, false);
			MemberAccessExpressionSyntax memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.IdentifierName(StringConstants.Assert), SyntaxFactory.IdentifierName(functionName)
					).WithOperatorToken(SyntaxFactory.Token(SyntaxKind.DotToken));
			SyntaxNode newExpression = SyntaxFactory.InvocationExpression(memberAccessExpression).WithArgumentList(newArguments);

			SyntaxNode newRoot = root.ReplaceNode(invocationExpression, newExpression);

			return document.WithSyntaxRoot(newRoot);
		}

		private async Task<Document> ReplaceEqualsEquals(Document document, InvocationExpressionSyntax invocationExpression, bool isIsTrue, SyntaxKind kind, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			ArgumentListSyntax newArguments = DecomposeEqualsEquals(kind, invocationExpression.ArgumentList, out bool isNotEquals, out bool isNullArgument);

			string functionName = DetermineFunction(isIsTrue, isNotEquals, isNullArgument);
			MemberAccessExpressionSyntax memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.IdentifierName(StringConstants.Assert), SyntaxFactory.IdentifierName(functionName)
					).WithOperatorToken(SyntaxFactory.Token(SyntaxKind.DotToken));
			SyntaxNode newExpression = SyntaxFactory.InvocationExpression(memberAccessExpression).WithArgumentList(newArguments);

			SyntaxTriviaList leading = invocationExpression.GetLeadingTrivia();

			SyntaxNode newNode = newExpression.WithLeadingTrivia(leading);
			SyntaxNode newRoot = root.ReplaceNode(invocationExpression, newNode);
			return document.WithSyntaxRoot(newRoot);
		}

		private string DetermineFunction(bool isIsTrue, bool isNotEquals, bool isNullArgument)
		{
			//Assert.<IsTrue|IsFalse>(arg1 <==|!=> arg2, ...)

			//four choices:

			//Assert.IsTrue(x == y) -> Assert.AreEqual(x, y)		(true, false) (if null, IsNull)
			//Assert.IsTrue(x != y) -> Assert.AreNotEqual(x, y)		(true, true) (if null, IsNotNull)
			//Assert.IsFalse(x == y) -> Assert.AreNotEqual(x, y)	(false, false) (if null, IsNotNull)
			//Assert.IsFalse(x != y) -> Assert.AreEqual(x, y)		(false, true) (if null, IsNull)

			if (isIsTrue)
			{
				if (isNotEquals)
				{
					return isNullArgument ? StringConstants.IsNotNullMethodName : StringConstants.AreNotEqualMethodName;
				}
				return isNullArgument ? StringConstants.IsNullMethodName : StringConstants.AreEqualMethodName;
			}
			if (isNotEquals)
			{
				return isNullArgument ? StringConstants.IsNullMethodName : StringConstants.AreEqualMethodName;
			}
			return isNullArgument ? StringConstants.IsNotNullMethodName : StringConstants.AreNotEqualMethodName;
		}

		private ArgumentListSyntax DecomposeEqualsFunction(ArgumentListSyntax argumentList, out bool isNotEquals)
		{
			ArgumentSyntax first = argumentList.Arguments[0];

			var rest = argumentList.Arguments.Skip(1).ToList();

			ExtractEqualsFunction(first, out ArgumentSyntax areEqualsExpected, out ArgumentSyntax areEqualsActual, out isNotEquals);

			SeparatedSyntaxList<ArgumentSyntax> newArguments = new();

			newArguments = newArguments.Add(areEqualsExpected);
			newArguments = newArguments.Add(areEqualsActual);
			newArguments = newArguments.AddRange(rest);

			return SyntaxFactory.ArgumentList(newArguments);
		}

		private ArgumentListSyntax DecomposeEqualsEquals(SyntaxKind kind, ArgumentListSyntax argumentList, out bool isNotEquals, out bool isNullArgument)
		{
			ArgumentSyntax first = argumentList.Arguments[0];

			var rest = argumentList.Arguments.Skip(1).ToList();

			isNotEquals = kind switch
			{
				SyntaxKind.EqualsExpression => false,
				SyntaxKind.NotEqualsExpression => true,
				_ => throw new ArgumentException("kind is not supported", nameof(kind)),
			};
			ExtractEqualsEquals(first, out ArgumentSyntax areEqualsExpected, out ArgumentSyntax areEqualsActual);

			SeparatedSyntaxList<ArgumentSyntax> newArguments = new();

			if (areEqualsActual.Expression.Kind() == SyntaxKind.NullLiteralExpression)
			{
				isNullArgument = true;
				newArguments = newArguments.Add(areEqualsExpected);
			}
			else if (areEqualsExpected.Expression.Kind() == SyntaxKind.NullLiteralExpression)
			{
				isNullArgument = true;
				newArguments = newArguments.Add(areEqualsActual);
			}
			else
			{
				isNullArgument = false;

				newArguments = newArguments.Add(areEqualsExpected);
				newArguments = newArguments.Add(areEqualsActual);
			}

			newArguments = newArguments.AddRange(rest);

			return SyntaxFactory.ArgumentList(newArguments);
		}

		private void ExtractEqualsEquals(ArgumentSyntax equalsEquals, out ArgumentSyntax areEqualsExpected, out ArgumentSyntax areEqualsActual)
		{
			var a = (BinaryExpressionSyntax)equalsEquals.Expression;

			areEqualsExpected = SyntaxFactory.Argument(a.Left);
			areEqualsActual = SyntaxFactory.Argument(a.Right);
		}

		private void ExtractEqualsFunction(ArgumentSyntax first, out ArgumentSyntax areEqualsExpected, out ArgumentSyntax areEqualsActual, out bool isNotEquals)
		{
			//something of the form x.Equals(y) pull out x and y.
			isNotEquals = false;

			var invocation = (InvocationExpressionSyntax)first.Expression;

			areEqualsActual = SyntaxFactory.Argument(invocation.ArgumentList.Arguments[0].Expression);

			var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

			areEqualsExpected = SyntaxFactory.Argument(memberAccess.Expression);
		}
	}
}
