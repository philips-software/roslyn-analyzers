// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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

			// Validate and extract string.Format information
			if (!TryExtractStringFormatInfo(node, out InvocationExpressionSyntax invocation, out var formatString, out ArgumentSyntax[] arguments))
			{
				return document;
			}

			// Verify this is actually a string.Format call
			if (!await IsStringFormatInvocation(document, invocation, cancellationToken))
			{
				return document;
			}

			// Find the parent interpolated string and apply the transformation
			return await ApplyInterpolatedStringTransformation(document, root, node, formatString, arguments);
		}

		private bool TryExtractStringFormatInfo(InterpolationSyntax node, out InvocationExpressionSyntax invocation, out string formatString, out ArgumentSyntax[] arguments)
		{
			invocation = null;
			formatString = null;
			arguments = null;

			// Get the string.Format invocation from the interpolation
			if (node.Expression is not InvocationExpressionSyntax inv)
			{
				return false;
			}

			invocation = inv;

			// Quick syntactic check for "Format" method name before expensive semantic analysis
			if (!IsFormatMethodSyntactically(invocation))
			{
				return false;
			}

			// Extract format string and arguments
			if (invocation.ArgumentList.Arguments.Count < 1)
			{
				return false;
			}

			ArgumentSyntax formatArgument = invocation.ArgumentList.Arguments[0];
			if (formatArgument.Expression is not LiteralExpressionSyntax formatLiteral ||
				!formatLiteral.Token.IsKind(SyntaxKind.StringLiteralToken))
			{
				return false;
			}

			formatString = formatLiteral.Token.ValueText;
			arguments = invocation.ArgumentList.Arguments.Skip(1).ToArray();
			return true;
		}

		private static bool IsFormatMethodSyntactically(InvocationExpressionSyntax invocation)
		{
			return invocation.Expression switch
			{
				MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText == "Format",
				IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "Format",
				_ => false
			};
		}

		private async Task<bool> IsStringFormatInvocation(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
		{
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
			return symbolInfo.Symbol is IMethodSymbol methodSymbol &&
				   methodSymbol.ContainingType?.SpecialType == SpecialType.System_String;
		}

		private Task<Document> ApplyInterpolatedStringTransformation(Document document, SyntaxNode root, InterpolationSyntax node, string formatString, ArgumentSyntax[] arguments)
		{
			// Find the parent interpolated string
			InterpolatedStringExpressionSyntax interpolatedString = node.FirstAncestorOrSelf<InterpolatedStringExpressionSyntax>();
			if (interpolatedString == null)
			{
				return Task.FromResult(document);
			}

			// Convert the string.Format to interpolated content
			List<InterpolatedStringContentSyntax> newContents = ConvertStringFormatToInterpolatedContents(formatString, arguments);

			// Get current contents and find the position of the node to replace
			var currentContents = interpolatedString.Contents.ToList();
			var index = currentContents.IndexOf(node);
			if (index == -1)
			{
				return Task.FromResult(document);
			}

			// Remove the current interpolation and insert the new contents
			currentContents.RemoveAt(index);
			if (newContents.Count > 0)
			{
				currentContents.InsertRange(index, newContents);
			}
			// If newContents is empty (empty format string case), we just remove the interpolation

			// Create new interpolated string with the updated contents
			InterpolatedStringExpressionSyntax newInterpolatedString = interpolatedString.WithContents(SyntaxFactory.List(currentContents));

			// Replace in the syntax tree
			root = root.ReplaceNode(interpolatedString, newInterpolatedString);

			return Task.FromResult(document.WithSyntaxRoot(root));
		}

		private List<InterpolatedStringContentSyntax> ConvertStringFormatToInterpolatedContents(string formatString, ArgumentSyntax[] arguments)
		{
			var result = new List<InterpolatedStringContentSyntax>();
			var placeholderPattern = new Regex(@"\{(\d+)(?::([^}]*))?\}",
				RegexOptions.None, TimeSpan.FromSeconds(1));
			var lastIndex = 0;

			MatchCollection matches = placeholderPattern.Matches(formatString);

			foreach (Match match in matches)
			{
				// Add text before the placeholder
				if (match.Index > lastIndex)
				{
					var textContent = formatString.Substring(lastIndex, match.Index - lastIndex);
					if (!string.IsNullOrEmpty(textContent))
					{
						SyntaxToken textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty,
							SyntaxKind.InterpolatedStringTextToken,
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
					// Invalid placeholder - return empty collection instead of null for graceful handling
					return [];
				}

				lastIndex = match.Index + match.Length;
			}

			// Add remaining text after all placeholders (or all text if no placeholders)
			if (lastIndex < formatString.Length)
			{
				var remainingText = formatString.Substring(lastIndex);
				if (!string.IsNullOrEmpty(remainingText))
				{
					SyntaxToken textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty,
						SyntaxKind.InterpolatedStringTextToken,
						remainingText, remainingText, SyntaxTriviaList.Empty);
					result.Add(SyntaxFactory.InterpolatedStringText(textToken));
				}
			}

			// Special case: if format string is empty or only contains text with no placeholders,
			// and we haven't added anything yet, add an empty text token to represent the content
			if (result.Count == 0 && matches.Count == 0)
			{
				if (string.IsNullOrEmpty(formatString))
				{
					// For empty format string, return empty list to remove the interpolation entirely
					return result;
				}
				else
				{
					// For non-empty format string with no placeholders, add the text
					SyntaxToken textToken = SyntaxFactory.Token(SyntaxTriviaList.Empty,
						SyntaxKind.InterpolatedStringTextToken,
						formatString, formatString, SyntaxTriviaList.Empty);
					result.Add(SyntaxFactory.InterpolatedStringText(textToken));
				}
			}

			return result;
		}
	}
}