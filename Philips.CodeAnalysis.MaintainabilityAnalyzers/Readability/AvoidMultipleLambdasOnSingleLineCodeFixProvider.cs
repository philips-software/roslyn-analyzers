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
			// Nice location to break the line, after the closing parenthesis of the ArgumentList where the first lambda is part of.
			return firstLambdaOnLine?.Parent?.Parent;
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

			// Calculate basic indentation to ensure proper formatting even if Formatter.Annotation fails
			var indentation = GetIndentationForNode(oldNode);

			SyntaxTriviaList newTrivia = lastToken.TrailingTrivia
				.Add(SyntaxFactory.EndOfLine(StringConstants.WindowsNewLine))
				.Add(SyntaxFactory.Whitespace(indentation));

			SyntaxNode newNode = oldNode.WithTrailingTrivia(newTrivia).WithAdditionalAnnotations(Formatter.Annotation);
			root = root.ReplaceNode(oldNode, newNode);

			return document.WithSyntaxRoot(root);
		}

		private string GetIndentationForNode(SyntaxNode node)
		{
			// Find the line that contains the node
			var lineStart = node.GetLocation().SourceSpan.Start;
			SyntaxTree syntaxTree = node.SyntaxTree;
			SourceText text = syntaxTree.GetText();
			TextLine line = text.Lines.GetLineFromPosition(lineStart);

			// Extract the existing indentation from the line
			var lineText = line.ToString();
			var indentationLength = 0;

			foreach (var c in lineText)
			{
				if (c is '\t' or ' ')
				{
					indentationLength++;
				}
				else
				{
					break;
				}
			}

			var baseIndentation = lineText.Substring(0, indentationLength);

			// Add one tab for the continuation line
			return baseIndentation + "\t";
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
