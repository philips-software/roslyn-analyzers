// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.
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

			// Find matching #endregion
			SyntaxToken token = node.ParentTrivia.Token;
			SyntaxTriviaList triviaList = token.LeadingTrivia;
			var regionIndex = triviaList
				.Select((t, i) => new { Trivia = t, Index = i })
				.FirstOrDefault(x => x.Trivia.HasStructure && x.Trivia.GetStructure() == node)?.Index ?? -1;

			if (regionIndex == -1)
			{
				return document;
			}

			var endRegionIndex = -1;
			EndRegionDirectiveTriviaSyntax endRegion = null;

			for (var i = regionIndex + 1; i < triviaList.Count; i++)
			{
				SyntaxNode structure = triviaList[i].GetStructure();
				if (structure is EndRegionDirectiveTriviaSyntax e)
				{
					endRegionIndex = i;
					endRegion = e;
					break;
				}
			}

			if (endRegion == null)
			{
				return document;
			}

			// Remove full lines from #region to #endregion
			var spanStart = node.GetLocation().SourceSpan.Start;
			var spanEnd = endRegion.GetLocation().SourceSpan.End;

			var fullText = text.ToString();
			var lineStart = text.Lines.GetLineFromPosition(spanStart).Span.Start;
			var lineEnd = text.Lines.GetLineFromPosition(spanEnd).Span.End;

			var spanToRemove = TextSpan.FromBounds(lineStart, lineEnd);

			SourceText newText = text.Replace(spanToRemove, "");
			SyntaxNode newRoot = root.SyntaxTree.WithChangedText(newText).GetRoot(cancellationToken);

			return document.WithSyntaxRoot(newRoot);
		}
	}
}

