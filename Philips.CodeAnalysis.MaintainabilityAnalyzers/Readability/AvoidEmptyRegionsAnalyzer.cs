// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var typeDeclaration = (TypeDeclarationSyntax)context.Node;

			// Skip analysis for nested classes to avoid false positives
			if (TypesHelper.IsNestedClass(typeDeclaration))
			{
				return;
			}

			// Get only the regions that are direct children of this type declaration
			System.Collections.Generic.List<DirectiveTriviaSyntax> directRegions = GetDirectRegions(typeDeclaration);
			if (directRegions.Count == 0)
			{
				return;
			}

			// Region directives should come in pairs
			if (directRegions.Count % 2 == 1)
			{
				return;
			}

			// If pair is malformed (eg. #region followed by #region), bail out
			for (var i = 0; i < directRegions.Count; i += 2)
			{
				DirectiveTriviaSyntax start = directRegions[i];
				DirectiveTriviaSyntax end = directRegions[i + 1];

				if (start.IsKind(SyntaxKind.EndRegionDirectiveTrivia) || end.IsKind(SyntaxKind.RegionDirectiveTrivia))
				{
					return;
				}
			}

			SyntaxList<MemberDeclarationSyntax> members = typeDeclaration.Members;

			// Check each region pair for emptiness
			for (var i = 0; i < directRegions.Count; i += 2)
			{
				DirectiveTriviaSyntax regionStart = directRegions[i];
				DirectiveTriviaSyntax regionEnd = directRegions[i + 1];

				Location regionLocation = regionStart.GetLocation();
				var regionStartLine = regionLocation.GetLineSpan().StartLinePosition.Line;
				var regionEndLine = regionEnd.GetLocation().GetLineSpan().StartLinePosition.Line;

				// Check if any members exist between the region lines
				var hasMembers = members.Any(member =>
				{
					var memberLine = member.GetLocation().GetLineSpan().StartLinePosition.Line;
					return memberLine > regionStartLine && memberLine < regionEndLine;
				});

				if (!hasMembers)
				{
					// Empty region - report diagnostic
					var diagnostic = Diagnostic.Create(AvoidEmpty, regionLocation);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		private static System.Collections.Generic.List<DirectiveTriviaSyntax> GetDirectRegions(TypeDeclarationSyntax typeDeclaration)
		{
			var regions = new System.Collections.Generic.List<DirectiveTriviaSyntax>();

			// Only get region directives that are directly under this type declaration,
			// not from nested types
			foreach (SyntaxTrivia trivia in typeDeclaration.DescendantTrivia())
			{
				if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) || trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
				{
					// Check if this trivia is directly under this type and not in a nested type
					SyntaxNode containingType = trivia.Token.Parent;
					while (containingType is not null and not TypeDeclarationSyntax)
					{
						containingType = containingType.Parent;
					}

					// Only include if the containing type is the current type we're analyzing
					if (containingType == typeDeclaration)
					{
						regions.Add((DirectiveTriviaSyntax)trivia.GetStructure());
					}
				}
			}

			return regions;
		}
	}
}