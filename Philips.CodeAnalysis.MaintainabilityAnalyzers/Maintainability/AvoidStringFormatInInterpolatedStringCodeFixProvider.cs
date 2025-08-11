// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
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
			}

			// Verify this is actually a string.Format call
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
			if (symbolInfo.Symbol is not IMethodSymbol methodSymbol ||
				methodSymbol.Name != "Format" ||
				methodSymbol.ContainingType?.SpecialType != SpecialType.System_String)
			{
				return document;
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
			}

			var formatString = formatLiteral.Token.ValueText;
			ArgumentSyntax[] arguments = invocation.ArgumentList.Arguments.Skip(1).ToArray();

			// Find the parent interpolated string
			InterpolatedStringExpressionSyntax interpolatedString = node.FirstAncestorOrSelf<InterpolatedStringExpressionSyntax>();
			if (interpolatedString == null)
			{
				return document;
			}

			// Convert the string.Format to interpolated content
			List<InterpolatedStringContentSyntax> newContents = ConvertStringFormatToInterpolatedContents(formatString, arguments);
			if (newContents == null)
			{
				return document;
			}

			// Replace the interpolation node with the new contents in the parent interpolated string
			var currentContents = interpolatedString.Contents.ToList();
			var index = currentContents.IndexOf(node);
			if (index == -1)
			{
				return document;
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
			var lastIndex = 0;

			foreach (Match match in placeholderPattern.Matches(formatString))
			{
				// Add text before the placeholder
				if (match.Index > lastIndex)
				{
					var textContent = formatString.Substring(lastIndex, match.Index - lastIndex);
					if (!string.IsNullOrEmpty(textContent))
					{
						SyntaxToken textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken,
							textContent, textContent, SyntaxTriviaList.Empty);
						result.Add(SyntaxFactory.InterpolatedStringText(textToken));
					}
				}

				// Parse the placeholder
				if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var argIndex) && argIndex < arguments.Length)
				{
					ExpressionSyntax expression = arguments[argIndex].Expression;
					InterpolationSyntax interpolation;

					// Add format specifier if present
					if (match.Groups[2].Success && !string.IsNullOrEmpty(match.Groups[2].Value))
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
				}
				else
				{
					// Invalid placeholder - shouldn't happen with valid string.Format, but handle gracefully
					return null;
				}

				lastIndex = match.Index + match.Length;
			}

			// Add remaining text
			if (lastIndex < formatString.Length)
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