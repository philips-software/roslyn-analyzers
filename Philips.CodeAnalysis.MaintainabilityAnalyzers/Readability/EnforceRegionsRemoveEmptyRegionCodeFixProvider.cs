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
			if (node is null || node.ParentTrivia.Token.RawKind == 0)
			{
				return document;
			}

			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

			SyntaxToken token = node.ParentTrivia.Token;
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

			var regionStart = node.GetLocation().SourceSpan.Start;
			var regionEnd = endRegion.GetLocation().SourceSpan.End;

			TextLine startLine = text.Lines.GetLineFromPosition(regionStart);
			TextLine endLine = text.Lines.GetLineFromPosition(regionEnd);

			var blankLineAbove = startLine.LineNumber > 0 && string.IsNullOrWhiteSpace(text.Lines[startLine.LineNumber - 1].ToString());
			var blankLineBelow = endLine.LineNumber < text.Lines.Count - 1 && string.IsNullOrWhiteSpace(text.Lines[endLine.LineNumber + 1].ToString());

			var actualStartLine = blankLineAbove && blankLineBelow ? startLine.LineNumber - 1 : startLine.LineNumber;

			var spanToRemove = TextSpan.FromBounds(
				text.Lines[actualStartLine].Start,
				endLine.SpanIncludingLineBreak.End
			);

			SourceText newText = text.Replace(spanToRemove, string.Empty);
			SyntaxNode newRoot = await root.SyntaxTree.WithChangedText(newText).GetRootAsync(cancellationToken).ConfigureAwait(false);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}

