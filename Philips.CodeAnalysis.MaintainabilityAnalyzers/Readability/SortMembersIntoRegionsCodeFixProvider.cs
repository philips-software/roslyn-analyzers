// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SortMembersIntoRegionsCodeFixProvider)), Shared]
	public class SortMembersIntoRegionsCodeFixProvider : SingleDiagnosticCodeFixProvider<TypeDeclarationSyntax>
	{
		protected override string Title => "Sort members into regions";

		protected override DiagnosticId DiagnosticId => DiagnosticId.SortMembersIntoRegions;

		protected override async Task<Document> ApplyFix(Document document, TypeDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			if (node == null || !MemberRegionClassifier.TryGetCurrentRegions(node, out _))
			{
				return document;
			}

			SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
			var eol = text.ToString().Contains("\r\n") ? "\r\n" : "\n";
			var typeIndent = GetIndentation(node.GetLeadingTrivia(), string.Empty);
			var memberIndent = GetIndentation(node.Members[0].GetLeadingTrivia(), typeIndent + "\t");

			var isTestClass = MemberRegionClassifier.IsTestClass(node);
			ILookup<string, MemberDeclarationSyntax> membersByRegion = node.Members.ToLookup(m => MemberRegionClassifier.GetExpectedRegion(m, isTestClass));

			var body = new StringBuilder();
			if (!node.OpenBraceToken.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
			{
				_ = body.Append(eol);
			}

			var isFirstRegion = true;
			foreach (var region in MemberRegionClassifier.RegionOrder.Where(membersByRegion.Contains))
			{
				if (!isFirstRegion)
				{
					_ = body.Append(eol);
				}
				isFirstRegion = false;

				_ = body.Append(memberIndent).Append("#region ").Append(MemberRegionClassifier.GetRegionName(region, properties)).Append(eol).Append(eol);
				MemberDeclarationSyntax previous = null;
				foreach (MemberDeclarationSyntax member in membersByRegion[region])
				{
					var isConsecutiveField = previous is FieldDeclarationSyntax && member is FieldDeclarationSyntax;
					if (previous != null && !isConsecutiveField)
					{
						_ = body.Append(eol);
					}
					previous = member;
					AppendMember(body, member, memberIndent, eol);
				}
				_ = body.Append(eol).Append(memberIndent).Append("#endregion").Append(eol);
			}

			_ = body.Append(typeIndent).Append(StripRegions(node.CloseBraceToken.LeadingTrivia).ToFullString());

			var span = TextSpan.FromBounds(node.OpenBraceToken.FullSpan.End, node.CloseBraceToken.SpanStart);
			SourceText newText = text.Replace(span, body.ToString());
			return document.WithText(newText);
		}

		private static void AppendMember(StringBuilder body, MemberDeclarationSyntax member, string indent, string eol)
		{
			var leading = StripRegions(member.GetLeadingTrivia()).ToFullString();
			var fullText = member.ToFullString();
			var rest = fullText.Substring(member.GetLeadingTrivia().FullSpan.Length);
			_ = body.Append(indent).Append(leading).Append(rest);
			if (!rest.EndsWith("\n"))
			{
				_ = body.Append(eol);
			}
		}

		private static SyntaxTriviaList StripRegions(SyntaxTriviaList trivia)
		{
			var kept = new List<SyntaxTrivia>();
			foreach (SyntaxTrivia t in trivia)
			{
				if (t.IsKind(SyntaxKind.RegionDirectiveTrivia) || t.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
				{
					if (kept.Count > 0 && kept[kept.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia))
					{
						kept.RemoveAt(kept.Count - 1);
					}
					continue;
				}
				kept.Add(t);
			}

			IEnumerable<SyntaxTrivia> withoutLeadingBlanks = kept.SkipWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));
			return SyntaxFactory.TriviaList(withoutLeadingBlanks);
		}

		private static string GetIndentation(SyntaxTriviaList trivia, string fallback)
		{
			if (trivia.Count > 0 && trivia[trivia.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia))
			{
				return trivia[trivia.Count - 1].ToString();
			}
			return fallback;
		}
	}
}
