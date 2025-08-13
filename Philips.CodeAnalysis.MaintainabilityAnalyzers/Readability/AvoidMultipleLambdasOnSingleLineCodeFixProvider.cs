// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidMultipleLambdasOnSingleLineCodeFixProvider)), Shared]
	public class AvoidMultipleLambdasOnSingleLineCodeFixProvider : SingleDiagnosticCodeFixProvider<SyntaxNode>
	{
		protected override string Title => "Put every lambda on a separate line";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidMultipleLambdasOnSingleLine;

		protected override SyntaxNode GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			SyntaxNode diagnosticNode = root.FindNode(diagnosticSpan);
			LambdaExpressionSyntax secondLambda = diagnosticNode.ChildNodes().OfType<LambdaExpressionSyntax>().FirstOrDefault();
			SyntaxNode parent = GetParentOnHigherLine(secondLambda);
			if (secondLambda is null || parent is null)
			{
				// Apparently, the code is different than what we expect.
				return null;
			}
			LambdaExpressionSyntax firstLambdaOnLine = GetOtherLambdaOnSameLine(parent, secondLambda);

			// For nested lambda scenarios (like Moq), we want to break before the second lambda
			// For LINQ chain scenarios, we want to break after the first lambda's method call

			// Check if this is a nested lambda scenario by looking at the immediate parent structure
			if (IsNestedLambdaScenario(firstLambdaOnLine, secondLambda))
			{
				// For nested lambdas, we want to break after the method call that contains the second lambda
				// Find the InvocationExpressionSyntax that directly contains the second lambda
				InvocationExpressionSyntax containingInvocation = secondLambda.Ancestors()
					.OfType<InvocationExpressionSyntax>()
					.FirstOrDefault(inv => inv.ArgumentList.Arguments.Any(arg => arg.DescendantNodes().Contains(secondLambda)));

				if (containingInvocation != null)
				{
					// Return the Expression part of the invocation (e.g., "It.Is<ICertificateInfo>")
					// so that the line break is added after it
					return containingInvocation.Expression;
				}

				// Fallback to the argument containing the second lambda
				return secondLambda.Parent;
			}

			// For LINQ chains, break after the first lambda's method call (existing logic)
			return firstLambdaOnLine?.Parent?.Parent;
		}

		private bool IsNestedLambdaScenario(LambdaExpressionSyntax firstLambda, LambdaExpressionSyntax secondLambda)
		{
			if (firstLambda == null || secondLambda == null)
			{
				return false;
			}

			// Check if the second lambda is a descendant of the first lambda
			// This indicates a nested scenario like Moq where one lambda contains another
			// Walking upwards from secondLambda is more efficient than traversing all descendants of firstLambda
			return secondLambda.Ancestors().Contains(firstLambda);
		}

		protected override async Task<Document> ApplyFix(Document document, SyntaxNode node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			SyntaxNode oldNode = node;
			SyntaxToken lastToken = oldNode.GetLastToken();
			// If the 2 lambdas are distinct statements, put the new line after the semicolon.
			if (lastToken.GetNextToken().IsKind(SyntaxKind.SemicolonToken))
			{
				lastToken = lastToken.GetNextToken();
				oldNode = lastToken.Parent;
			}

			// Use the simpler approach: let Roslyn's formatter handle indentation automatically
			SyntaxNode newNode = oldNode.WithTrailingTrivia(
				lastToken.TrailingTrivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed)
			).WithAdditionalAnnotations(Formatter.Annotation);

			root = root.ReplaceNode(oldNode, newNode);
			Document newDocument = document.WithSyntaxRoot(root);

			// Let Roslyn format the entire document to ensure proper indentation
			return await Formatter.FormatAsync(newDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
		}



		private SyntaxNode GetParentOnHigherLine(SyntaxNode node)
		{
			var lineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line;
			return node
				.Ancestors()
				.FirstOrDefault(l => l.GetLocation().GetLineSpan().StartLinePosition.Line < lineNumber);
		}

		private LambdaExpressionSyntax GetOtherLambdaOnSameLine(SyntaxNode parent, LambdaExpressionSyntax second)
		{
			var lineNumber = second.GetLocation().GetLineSpan().StartLinePosition.Line;
			return parent
				.DescendantNodes()
				.OfType<LambdaExpressionSyntax>()
				.Where(k => !ReferenceEquals(k, second))
				.FirstOrDefault(l => l.GetLocation().GetLineSpan().StartLinePosition.Line == lineNumber);
		}
	}
}
