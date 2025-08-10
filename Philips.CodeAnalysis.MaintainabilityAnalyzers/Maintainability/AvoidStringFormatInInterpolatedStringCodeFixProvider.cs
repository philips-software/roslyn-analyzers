// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp,
		Name = nameof(AvoidStringFormatInInterpolatedStringCodeFixProvider)), Shared]
	public class AvoidStringFormatInInterpolatedStringCodeFixProvider :
		SingleDiagnosticCodeFixProvider<InterpolationSyntax>
	{
		protected override string Title => "Remove redundant string.Format call";

		protected override DiagnosticId DiagnosticId =>
			DiagnosticId.AvoidStringFormatInInterpolatedString;

		protected override async Task<Document> ApplyFix(Document document, InterpolationSyntax node,
			ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			// Get the string.Format invocation from the interpolation
			if (node.Expression is not InvocationExpressionSyntax invocation)
			{
				return document;
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			}

			// Verify this is actually a string.Format call
After:
			}

			// Verify this is actually a string.Format call
*/

			}

			// Verify this is actually a string.Format call
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
			if (symbolInfo.Symbol is not IMethodSymbol methodSymbol ||
				methodSymbol.Name != "Format" ||
				methodSymbol.ContainingType?.SpecialType != SpecialType.System_String)
			{
				return document;
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			}

			// Extract format string and arguments
After:
			}

			// Extract format string and arguments
*/

			}

			// Extract format string and arguments
			if (invocation.ArgumentList.Arguments.Count < 1)
			{
				return document;
			}

			ArgumentSyntax formatArgument = invocation.ArgumentList.Arguments[0];
			if (formatArgument.Expression is not LiteralExpressionSyntax formatLiteral ||
				!formatLiteral.Token.IsKind(SyntaxKind.StringLiteralToken))
			{
				return document;
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			string formatString = formatLiteral.Token.ValueText;
After:
			var formatString = formatLiteral.Token.ValueText;
*/

			}

			var formatString = formatLiteral.Token.ValueText;
			ArgumentSyntax[] arguments = invocation.ArgumentList.Arguments.Skip(1).ToArray();

			// Find the parent interpolated string
			InterpolatedStringExpressionSyntax interpolatedString = node.FirstAncestorOrSelf<InterpolatedStringExpressionSyntax>();
			if (interpolatedString == null)
			{
				return document;
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			}

			// Convert the string.Format to interpolated content
After:
			}

			// Convert the string.Format to interpolated content
*/

			}

			// Convert the string.Format to interpolated content
			List<InterpolatedStringContentSyntax> newContents = ConvertStringFormatToInterpolatedContents(formatString, arguments);
			if (newContents == null)
			{
				return document;
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			}

			// Replace the interpolation node with the new contents in the parent interpolated string
After:
			}

			// Replace the interpolation node with the new contents in the parent interpolated string
*/

			}

			// Replace the interpolation node with the new contents in the parent interpolated string
			var currentContents = interpolatedString.Contents.ToList();
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			int index = currentContents.IndexOf(node);
After:
			var index = currentContents.IndexOf(node);
*/

			var index = currentContents.IndexOf(node);
			if (index == -1)
			{
				return document;
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			}

			// Remove the current interpolation and insert the new contents
After:
			}

			// Remove the current interpolation and insert the new contents
*/

			}

			// Remove the current interpolation and insert the new contents
			currentContents.RemoveAt(index);
			currentContents.InsertRange(index, newContents);

			// Create new interpolated string with the updated contents
			InterpolatedStringExpressionSyntax newInterpolatedString = interpolatedString.WithContents(SyntaxFactory.List(currentContents));

			// Replace in the syntax tree
			root = root.ReplaceNode(interpolatedString, newInterpolatedString);

			return document.WithSyntaxRoot(root);
		}

		private List<InterpolatedStringContentSyntax> ConvertStringFormatToInterpolatedContents(string formatString, ArgumentSyntax[] arguments)
		{
			var result = new List<InterpolatedStringContentSyntax>();
			var placeholderPattern = new Regex(@"\{(\d+)(?::([^}]*))?\}");
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			int lastIndex = 0;
After:
			var lastIndex = 0;
*/

			var lastIndex = 0;

			foreach (Match match in placeholderPattern.Matches(formatString))
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			{
				// Add text before the placeholder
After:
			{
				// Add text before the placeholder
*/

			{
				// Add text before the placeholder
				if (match.Index > lastIndex)
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
					string textContent = formatString.Substring(lastIndex, match.Index - lastIndex);
After:
					var textContent = formatString.Substring(lastIndex, match.Index - lastIndex);
*/

				{
					var textContent = formatString.Substring(lastIndex, match.Index - lastIndex);
					if (!string.IsNullOrEmpty(textContent))
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
						var textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken,
After:
						SyntaxToken textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken,
*/

					{
						SyntaxToken textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken,
							textContent, textContent, SyntaxTriviaList.Empty);
						result.Add(SyntaxFactory.InterpolatedStringText(textToken));
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
				}

				// Parse the placeholder
After:
				}

				// Parse the placeholder
*/

					}
				}

				// Parse the placeholder
				if (int.TryParse(match.Groups[1].Value, out var argIndex) && argIndex < arguments.Length)
				{
					ExpressionSyntax expression = arguments[argIndex].Expression;
					InterpolationSyntax interpolation;

					// Add format specifier if present
					if (match.Groups[2].Success && !string.IsNullOrEmpty(match.Groups[2].Value))
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
						var formatClause = SyntaxFactory.InterpolationFormatClause(
After:
						InterpolationFormatClauseSyntax formatClause = SyntaxFactory.InterpolationFormatClause(
*/

					{
						InterpolationFormatClauseSyntax formatClause = SyntaxFactory.InterpolationFormatClause(
							SyntaxFactory.Token(SyntaxKind.ColonToken),
							SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken,
								match.Groups[2].Value, match.Groups[2].Value, SyntaxTriviaList.Empty));
						interpolation = SyntaxFactory.Interpolation(expression, null, formatClause);
					}
					else
					{
						interpolation = SyntaxFactory.Interpolation(expression);
					}

					result.Add(interpolation);
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
				{
					// Invalid placeholder - shouldn't happen with valid string.Format, but handle gracefully
After:
				{
					// Invalid placeholder - shouldn't happen with valid string.Format, but handle gracefully
*/

				}
				else
				{
					// Invalid placeholder - shouldn't happen with valid string.Format, but handle gracefully
					return null;
				}

				lastIndex = match.Index + match.Length;
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
			}

			// Add remaining text
After:
			}

			// Add remaining text
*/

			}

			// Add remaining text
			if (lastIndex < formatString.Length)
/* Unmerged change from project 'Philips.CodeAnalysis.MaintainabilityAnalyzers(netstandard2.0)'
Before:
				string remainingText = formatString.Substring(lastIndex);
				if (!string.IsNullOrEmpty(remainingText))
				{
					var textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken,
After:
				var remainingText = formatString.Substring(lastIndex);
				if (!string.IsNullOrEmpty(remainingText))
				{
					SyntaxToken textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken,
*/

			{
				var remainingText = formatString.Substring(lastIndex);
				if (!string.IsNullOrEmpty(remainingText))
				{
					SyntaxToken textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken,
						remainingText, remainingText, SyntaxTriviaList.Empty);
					result.Add(SyntaxFactory.InterpolatedStringText(textToken));
				}
			}

			return result;
		}
	}
}
