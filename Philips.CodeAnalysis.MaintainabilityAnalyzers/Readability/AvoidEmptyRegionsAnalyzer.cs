// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidEmptyRegionsAnalyzer : DiagnosticAnalyzerBase
	{
		private const string AvoidEmptyRegionTitle = @"Avoid empty regions";
		public const string AvoidEmptyRegionMessageFormat = @"Remove the empty region";
		private const string AvoidEmptyRegionDescription = @"Remove the empty region";
		private const string AvoidEmptyRegionCategory = Categories.Readability;

		private static readonly DiagnosticDescriptor AvoidEmpty = new(DiagnosticId.AvoidEmptyRegions.ToId(), AvoidEmptyRegionTitle,
			AvoidEmptyRegionMessageFormat, AvoidEmptyRegionCategory, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AvoidEmptyRegionDescription);

		private static readonly Regex CopyrightRegex = new(@"©|\uFFFD|Copyright", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
		private static readonly Regex YearRegex = new(@"\d\d\d\d", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AvoidEmpty);

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.RegionDirectiveTrivia);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var regionDirective = (RegionDirectiveTriviaSyntax)context.Node;

			// Find the matching #endregion
			EndRegionDirectiveTriviaSyntax endRegionDirective = FindMatchingEndRegion(regionDirective);
			if (endRegionDirective == null)
			{
				return; // No matching end region found
			}

			// Check if the region is empty (contains only whitespace and comments)
			if (IsRegionEmpty(regionDirective, endRegionDirective, context.SemanticModel.SyntaxTree.GetText()))
			{
				var diagnostic = Diagnostic.Create(AvoidEmpty, regionDirective.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}

		private static EndRegionDirectiveTriviaSyntax FindMatchingEndRegion(RegionDirectiveTriviaSyntax regionStart)
		{
			SyntaxTree syntaxTree = regionStart.SyntaxTree;
			SyntaxNode root = syntaxTree.GetRoot();

			var regionDepth = 1;
			var startPosition = regionStart.SpanStart;

			// Find all region and endregion directives after the current region
			IOrderedEnumerable<SyntaxTrivia> allDirectives = root.DescendantTrivia(descendIntoTrivia: true)
				.Where(trivia => trivia.SpanStart > startPosition &&
								 (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
								  trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia)))
				.OrderBy(trivia => trivia.SpanStart);

			foreach (SyntaxTrivia trivia in allDirectives)
			{
				if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
				{
					regionDepth++;
				}
				else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
				{
					regionDepth--;
					if (regionDepth == 0)
					{
						return (EndRegionDirectiveTriviaSyntax)trivia.GetStructure();
					}
				}
			}

			return null;
		}

		private static bool IsRegionEmpty(RegionDirectiveTriviaSyntax regionStart, EndRegionDirectiveTriviaSyntax regionEnd, SourceText sourceText)
		{
			var regionStartLine = regionStart.GetLocation().GetLineSpan().StartLinePosition.Line;
			var regionEndLine = regionEnd.GetLocation().GetLineSpan().StartLinePosition.Line;

			var hasNonCommentContent = false;
			var hasCommentContent = false;

			// Check each line between the region start and end
			for (var lineNumber = regionStartLine + 1; lineNumber < regionEndLine; lineNumber++)
			{
				TextLine line = sourceText.Lines[lineNumber];
				var lineText = line.ToString().Trim();

				// Skip empty lines
				if (string.IsNullOrEmpty(lineText))
				{
					continue;
				}

				// If we find any non-comment line, the region is definitely not empty
				if (!lineText.StartsWith("//"))
				{
					hasNonCommentContent = true;
					break;
				}

				// Track that we have comment content
				hasCommentContent = true;
			}

			// If we have non-comment content, the region is not empty
			if (hasNonCommentContent)
			{
				return false;
			}

			// If we have no content at all (only whitespace), the region is empty
			if (!hasCommentContent)
			{
				return true;
			}

			// We have only comments - check if they contain copyright information
			return !ContainsCopyrightInformation(regionStart, regionEnd, sourceText);
		}

		private static bool ContainsCopyrightInformation(RegionDirectiveTriviaSyntax regionStart, EndRegionDirectiveTriviaSyntax regionEnd, SourceText sourceText)
		{
			var regionStartLine = regionStart.GetLocation().GetLineSpan().StartLinePosition.Line;
			var regionEndLine = regionEnd.GetLocation().GetLineSpan().StartLinePosition.Line;

			var allCommentText = string.Empty;

			// Collect all comment text in the region
			for (var lineNumber = regionStartLine + 1; lineNumber < regionEndLine; lineNumber++)
			{
				TextLine line = sourceText.Lines[lineNumber];
				var lineText = line.ToString().Trim();

				if (!string.IsNullOrEmpty(lineText) && lineText.StartsWith("//"))
				{
					allCommentText += lineText + " ";
				}
			}

			// Check if the comments contain copyright information
			var hasCopyright = CopyrightRegex.IsMatch(allCommentText);
			var hasYear = YearRegex.IsMatch(allCommentText);

			return hasCopyright && hasYear;
		}
	}
}
