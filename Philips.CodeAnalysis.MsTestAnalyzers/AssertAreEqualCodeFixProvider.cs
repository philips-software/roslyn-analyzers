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

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssertAreEqualCodeFixProvider)), Shared]
	public class AssertAreEqualCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Refactor equality assertion";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AssertAreEqual)); }
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
				SyntaxNode syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
				if (syntaxNode != null)
				{
					InvocationExpressionSyntax invocationExpression = syntaxNode.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

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
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);

			bool isFirstArgumentNull = false;
			bool isFirstArgumentConstant = false;
			ArgumentListSyntax argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
			LiteralExpressionSyntax arg0Literal = argumentList.Arguments[0].Expression as LiteralExpressionSyntax;
			if (arg0Literal != null)
			{
				Optional<object> literalValue = semanticModel.GetConstantValue(arg0Literal);
				isFirstArgumentNull = literalValue.Value == null;
				isFirstArgumentConstant = true;
			}
			else
			{
				isFirstArgumentConstant = Helper.IsConstantExpression(argumentList.Arguments[0].Expression, semanticModel);
			}

			bool isSecondArgumentNull = false;
			bool isSecondArgumentConstant = false;
			LiteralExpressionSyntax arg1Literal = argumentList.Arguments[1].Expression as LiteralExpressionSyntax;
			if (arg1Literal != null)
			{
				Optional<object> literalValue = semanticModel.GetConstantValue(arg1Literal);
				isSecondArgumentNull = literalValue.Value == null;
				isSecondArgumentConstant = true;
			}
			else
			{
				isSecondArgumentConstant = Helper.IsConstantExpression(argumentList.Arguments[1].Expression, semanticModel);
			}

			if (isFirstArgumentNull || isSecondArgumentNull)
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
				MemberAccessExpressionSyntax memberAccess = (MemberAccessExpressionSyntax)invocationExpression.Expression;
				string memberName = memberAccess.Name.ToString();
				if (memberName == @"AreEqual")
				{
					identifier = SyntaxFactory.ParseName(@"IsNull");
				}
				else
				{
					identifier = SyntaxFactory.ParseName(@"IsNotNull");
				}

				MemberAccessExpressionSyntax newMemberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, memberAccess.Expression, (SimpleNameSyntax)identifier);
				ArgumentListSyntax newArguments;

				if (argumentList.Arguments.Count == 2)
				{
					newArguments = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(new ArgumentSyntax[] { argument }));
				}
				else
				{
					// make sure not to delete any custom error message
					newArguments = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(new ArgumentSyntax[] { argument, argumentList.Arguments[2] }));
				}

				InvocationExpressionSyntax newInvocationExpression = SyntaxFactory.InvocationExpression(newMemberAccess, newArguments);
				SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
				SyntaxNode newRoot = oldRoot.ReplaceNode(invocationExpression, newInvocationExpression);
				document = document.WithSyntaxRoot(newRoot);
			}
			else if (isSecondArgumentConstant && !isFirstArgumentConstant)
			{
				// swap the argument list
				ArgumentListSyntax newArgumentList = argumentList.ReplaceNodes(new SyntaxNode[] { argumentList.Arguments[0], argumentList.Arguments[1] },
					(original, other) => original == argumentList.Arguments[0] ? argumentList.Arguments[1] : argumentList.Arguments[0]);

				SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
				SyntaxNode newRoot = oldRoot.ReplaceNode(argumentList, newArgumentList);
				document = document.WithSyntaxRoot(newRoot);
			}

			return document;
		}
	}
}