// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnforceRegionsRemoveEmptyRegionCodeFixProvider)), Shared]
	public class EnforceRegionsRemoveEmptyRegionCodeFixProvider : SingleDiagnosticCodeFixProvider<RegionDirectiveTriviaSyntax>
	{
		protected override string Title => "Remove empty #region";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidEmptyRegions;

		protected override RegionDirectiveTriviaSyntax GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			SyntaxToken token = root.FindToken(diagnosticSpan.Start);

			return token.LeadingTrivia
				.Select(t => t.GetStructure())
				.OfType<RegionDirectiveTriviaSyntax>()
				.FirstOrDefault(r => r.FullSpan.Contains(diagnosticSpan));
		}

		protected override async Task<Document> ApplyFix(Document document, RegionDirectiveTriviaSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

			// Defensive: node must be attached to a valid token
			SyntaxToken token = node.ParentTrivia.Token;
			if (token.RawKind == 0)
			{
				return document;
			}

			// Find matching #endregion
			SyntaxTriviaList triviaList = token.LeadingTrivia;
			var regionIndex = triviaList
				.Select((t, i) => new { Trivia = t, Index = i })
				.FirstOrDefault(x => x.Trivia.HasStructure && x.Trivia.GetStructure() == node)?.Index ?? -1;

			if (regionIndex < 0)
			{
				return document;
			}

			EndRegionDirectiveTriviaSyntax endRegion = null;
			var endRegionIndex = -1;

			for (var i = regionIndex + 1; i < triviaList.Count; i++)
			{
				SyntaxNode structure = triviaList[i].GetStructure();
				if (structure is EndRegionDirectiveTriviaSyntax end)
				{
					endRegion = end;
					endRegionIndex = i;
					break;
				}
			}

			if (endRegion == null || endRegionIndex <= regionIndex)
			{
				return document;
			}

			// Compute line spans to remove
			var regionStart = node.GetLocation().SourceSpan.Start;
			var endRegionEnd = endRegion.GetLocation().SourceSpan.End;

			var startLine = text.Lines.GetLineFromPosition(regionStart).LineNumber;
			var endLine = text.Lines.GetLineFromPosition(endRegionEnd).LineNumber;

			// Optional: eliminate one extra blank line if both sides are blank
			var canCollapseExtraLine = false;
			if (startLine > 0 && endLine < text.Lines.Count - 1)
			{
				var lineBefore = text.Lines[startLine - 1].ToString();
				var lineAfter = text.Lines[endLine + 1].ToString();
				if (string.IsNullOrWhiteSpace(lineBefore) && string.IsNullOrWhiteSpace(lineAfter))
				{
					canCollapseExtraLine = true;
					startLine--; // expand removal upward
				}
			}

			var spanToRemove = TextSpan.FromBounds(
				text.Lines[startLine].Start,
				text.Lines[endLine].End
			);

			SourceText newText = text.Replace(spanToRemove, canCollapseExtraLine ? Environment.NewLine : string.Empty);

			SyntaxNode newRoot = await root.SyntaxTree.WithChangedText(newText).GetRootAsync(cancellationToken).ConfigureAwait(false);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}

