// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
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

			// Check each line between the region start and end
			for (var lineNumber = regionStartLine + 1; lineNumber < regionEndLine; lineNumber++)
			{
				TextLine line = sourceText.Lines[lineNumber];
				var lineText = line.ToString().Trim();

				// If we find any non-empty, non-comment line, the region is not empty
				if (!string.IsNullOrEmpty(lineText) && !lineText.StartsWith("//"))
				{
					return false;
				}
			}

			return true; // Region contains only whitespace and/or comments
		}
	}
}