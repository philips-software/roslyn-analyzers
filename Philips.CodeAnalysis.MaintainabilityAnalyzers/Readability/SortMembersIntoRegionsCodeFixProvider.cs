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
		private static readonly MemberRegionClassifier Classifier = new();

		protected override string Title => "Sort members into regions";

		protected override DiagnosticId DiagnosticId => DiagnosticId.SortMembersIntoRegions;

		protected override async Task<Document> ApplyFix(Document document, TypeDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			if (node == null || !Classifier.TryGetCurrentRegions(node, out _))
			{
				return document;
			}

			SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
			Layout layout = TryPlanLayout(node, text, properties);
			if (layout == null)
			{
				return document;
			}

			var span = TextSpan.FromBounds(node.OpenBraceToken.FullSpan.End, node.CloseBraceToken.SpanStart);
			return document.WithText(text.Replace(span, BuildBody(layout)));
		}

		/// <summary>
		/// Decides which members go into which section, or returns null if regrouping is not safe.
		/// </summary>
		private static Layout TryPlanLayout(TypeDeclarationSyntax node, SourceText text, ImmutableDictionary<string, string> properties)
		{
			List<OriginalRegion> originalRegions = FindOriginalRegions(node, text);
			_ = properties.TryGetValue(MemberRegionClassifier.AllowedRegionsKey, out var allowedSetting);
			HashSet<string> allowedNames = Classifier.ParseAllowedRegions(allowedSetting);
			var allowedBlocks = originalRegions.Where(r => r.IsOutermost(allowedNames)).ToList();
			if (allowedBlocks.Exists(r => r.LosesComments))
			{
				return null;
			}

			var keptMembers = new HashSet<MemberDeclarationSyntax>(allowedBlocks.SelectMany(r => r.Members));
			var isTestClass = Classifier.IsTestClass(node);
			ILookup<string, MemberDeclarationSyntax> membersByRegion = node.Members
				.Where(m => !keptMembers.Contains(m))
				.ToLookup(m => Classifier.GetExpectedRegion(m, isTestClass));

			_ = properties.TryGetValue(MemberRegionClassifier.RegionOrderKey, out var orderSetting);
			IReadOnlyList<string> sectionOrder = Classifier.ParseSectionOrder(orderSetting);
			IEnumerable<MemberDeclarationSyntax> newOrder = sectionOrder.SelectMany(section =>
				section == MemberRegionClassifier.AllowedRegionsSection ? allowedBlocks.SelectMany(r => r.Members) : membersByRegion[section]);
			if (ChangesOrderOfSensitiveMembers(node, newOrder))
			{
				return null;
			}

			return new Layout(node, text, properties, originalRegions, allowedBlocks, membersByRegion, sectionOrder);
		}

		private static string BuildBody(Layout layout)
		{
			TypeDeclarationSyntax node = layout.Node;
			var body = new StringBuilder();
			if (!node.OpenBraceToken.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
			{
				_ = body.Append(layout.Eol);
			}

			IEnumerable<string> sections = layout.SectionOrder.SelectMany(section => RenderSection(layout, section));
			_ = body.Append(string.Join(layout.Eol, sections));
			_ = body.Append(layout.TypeIndent).Append(StripRegions(node.CloseBraceToken.LeadingTrivia).ToFullString());
			return body.ToString();
		}

		private static IEnumerable<string> RenderSection(Layout layout, string section)
		{
			if (section == MemberRegionClassifier.AllowedRegionsSection)
			{
				return layout.AllowedBlocks.Select(block => layout.Text.ToString(block.BlockSpan));
			}

			if (!layout.MembersByRegion.Contains(section))
			{
				return [];
			}

			var regionName = Classifier.GetRegionName(section, layout.Properties);
			var members = layout.MembersByRegion[section].ToList();
			OriginalRegion unchanged = layout.OriginalRegions.Find(r => r.IsSimple && r.Name == regionName && r.Members.SequenceEqual(members));
			if (unchanged != null)
			{
				return [layout.Text.ToString(unchanged.Span)];
			}

			var eol = layout.Eol;
			var indent = layout.MemberIndent;
			var region = new StringBuilder();
			_ = region.Append(indent).Append("#region ").Append(regionName).Append(eol).Append(eol);
			AppendMembers(region, layout.Node, members, indent, eol);
			_ = region.Append(eol).Append(indent).Append("#endregion").Append(eol);
			return [region.ToString()];
		}

		private sealed class Layout
		{
			public Layout(TypeDeclarationSyntax node, SourceText text, ImmutableDictionary<string, string> properties, List<OriginalRegion> originalRegions,
				List<OriginalRegion> allowedBlocks, ILookup<string, MemberDeclarationSyntax> membersByRegion, IReadOnlyList<string> sectionOrder)
			{
				Node = node;
				Text = text;
				Properties = properties;
				OriginalRegions = originalRegions;
				AllowedBlocks = allowedBlocks;
				MembersByRegion = membersByRegion;
				SectionOrder = sectionOrder;
				Eol = text.ToString().Contains("\r\n") ? "\r\n" : "\n";
				TypeIndent = GetIndentation(node.GetLeadingTrivia(), string.Empty);
				MemberIndent = GetIndentation(node.Members[0].GetLeadingTrivia(), TypeIndent + "\t");
			}

			public TypeDeclarationSyntax Node { get; }

			public SourceText Text { get; }

			public ImmutableDictionary<string, string> Properties { get; }

			public List<OriginalRegion> OriginalRegions { get; }

			public List<OriginalRegion> AllowedBlocks { get; }

			public ILookup<string, MemberDeclarationSyntax> MembersByRegion { get; }

			public IReadOnlyList<string> SectionOrder { get; }

			public string Eol { get; }

			public string TypeIndent { get; }

			public string MemberIndent { get; }
		}

		/// <summary>
		/// Static and instance initializers run as separate sequences, so their relative order is compared per group.
		/// </summary>
		private static bool ChangesOrderOfSensitiveMembers(TypeDeclarationSyntax node, IEnumerable<MemberDeclarationSyntax> newOrder)
		{
			foreach (var isStatic in new[] { true, false })
			{
				bool InGroup(MemberDeclarationSyntax m)
				{
					return Classifier.IsOrderSensitive(m, node) && m.Modifiers.Any(SyntaxKind.StaticKeyword) == isStatic;
				}

				if (!node.Members.Where(InGroup).SequenceEqual(newOrder.Where(InGroup)))
				{
					return true;
				}
			}
			return false;
		}

		private static void AppendMembers(StringBuilder body, TypeDeclarationSyntax node, List<MemberDeclarationSyntax> members, string indent, string eol)
		{
			MemberDeclarationSyntax previous = null;
			foreach (MemberDeclarationSyntax member in members)
			{
				var blankLines = GetBlankLinesBefore(node, previous, member);
				for (var i = 0; i < blankLines; i++)
				{
					_ = body.Append(eol);
				}
				previous = member;
				AppendMember(body, member, indent, eol);
			}
		}

		/// <summary>
		/// Members that keep their original predecessor keep their original spacing; moved members get one blank line, except between fields.
		/// </summary>
		private static int GetBlankLinesBefore(TypeDeclarationSyntax node, MemberDeclarationSyntax previous, MemberDeclarationSyntax member)
		{
			if (previous == null)
			{
				return 0;
			}

			SyntaxTriviaList leading = member.GetLeadingTrivia();
			var keptPredecessor = node.Members.IndexOf(member) == node.Members.IndexOf(previous) + 1 && !HasRegionDirective(leading);
			if (keptPredecessor)
			{
				return leading.TakeWhile(IsWhitespaceOrEndOfLine).Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
			}

			return previous is FieldDeclarationSyntax && member is FieldDeclarationSyntax ? 0 : 1;
		}

		private static List<OriginalRegion> FindOriginalRegions(TypeDeclarationSyntax node, SourceText text)
		{
			var result = new List<OriginalRegion>();
			var open = new Stack<OriginalRegion>();
			foreach (MemberDeclarationSyntax member in node.Members)
			{
				TrackRegions(member.GetLeadingTrivia(), text, open, result);
				foreach (OriginalRegion region in open)
				{
					region.Members.Add(member);
				}
			}
			TrackRegions(node.CloseBraceToken.LeadingTrivia, text, open, result);
			return result;
		}

		private static void TrackRegions(SyntaxTriviaList trivia, SourceText text, Stack<OriginalRegion> open, List<OriginalRegion> result)
		{
			var hasContent = false;
			var hasContentBeforeDirective = false;
			int? contentStart = null;
			foreach (SyntaxTrivia t in trivia)
			{
				if (t.IsKind(SyntaxKind.RegionDirectiveTrivia))
				{
					OriginalRegion parent = open.Count > 0 ? open.Peek() : null;
					parent?.IsSimple = false;
					var name = Classifier.GetDirectiveName((RegionDirectiveTriviaSyntax)t.GetStructure());
					var start = text.Lines.GetLineFromPosition(t.SpanStart).Start;
					var blockStart = text.Lines.GetLineFromPosition(contentStart ?? t.SpanStart).Start;
					open.Push(new OriginalRegion(name, parent, start, blockStart, !hasContent, hasContentBeforeDirective));
				}
				else if (t.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
				{
					OriginalRegion region = open.Pop();
					region.End = text.Lines.GetLineFromPosition(t.SpanStart).EndIncludingLineBreak;
					if (region.Members.Count > 0)
					{
						result.Add(region);
					}
				}
				else if (!IsWhitespaceOrEndOfLine(t))
				{
					hasContent = true;
					contentStart ??= t.SpanStart;
					continue;
				}
				else
				{
					continue;
				}

				hasContentBeforeDirective |= contentStart != null;
				contentStart = null;
			}
		}

		private static bool HasRegionDirective(SyntaxTriviaList trivia)
		{
			return trivia.Any(SyntaxKind.RegionDirectiveTrivia) || trivia.Any(SyntaxKind.EndRegionDirectiveTrivia);
		}

		private static bool IsWhitespaceOrEndOfLine(SyntaxTrivia trivia)
		{
			return trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia);
		}

		private sealed class OriginalRegion
		{
			public OriginalRegion(string name, OriginalRegion parent, int start, int blockStart, bool isSimple, bool losesComments)
			{
				Name = name;
				Parent = parent;
				Start = start;
				BlockStart = blockStart;
				IsSimple = isSimple;
				LosesComments = losesComments;
			}

			public string Name { get; }

			public OriginalRegion Parent { get; }

			public int Start { get; }

			// Includes comments directly above the #region line.
			public int BlockStart { get; }

			public int End { get; set; }

			// A standard region is copied verbatim only if it has no nested regions and no comments precede its #region line.
			public bool IsSimple { get; set; }

			// Comments that precede an earlier directive in the same trivia would be dropped when copying the block.
			public bool LosesComments { get; }

			public List<MemberDeclarationSyntax> Members { get; } = [];

			public TextSpan Span => TextSpan.FromBounds(Start, End);

			public TextSpan BlockSpan => TextSpan.FromBounds(BlockStart, End);

			public bool IsOutermost(HashSet<string> names)
			{
				if (!names.Contains(Name))
				{
					return false;
				}

				for (OriginalRegion ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
				{
					if (names.Contains(ancestor.Name))
					{
						return false;
					}
				}
				return true;
			}
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
