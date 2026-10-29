// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
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
	public class SortMembersIntoRegionsAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Sort members into regions";
		public const string MessageFormat = @"Members of '{0}' are not sorted into regions: '{1}' belongs in '#region {2}'";
		private const string Description = @"Members should be grouped into the regions 'Non-Public Data Members', 'Non-Public Properties/Methods' and 'Public Interface'. Test classes additionally use 'Test Methods', 'Test Mocks' and 'Test Classes'.";

		private static readonly MemberRegionClassifier Classifier = new();

		public SortMembersIntoRegionsAnalyzer()
			: base(DiagnosticId.SortMembersIntoRegions, Title, MessageFormat, Description, Categories.Readability, isEnabled: false)
		{ }

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			HashSet<string> allowedRegions = Classifier.ParseAllowedRegions(Helper.ForAdditionalFiles.GetValueFromEditorConfig(Rule.Id, MemberRegionClassifier.AllowedRegionsKey));
			ImmutableDictionary<string, string> regionNames = Classifier
				.LoadRegionNames(key => Helper.ForAdditionalFiles.GetValueFromEditorConfig(Rule.Id, key))
				.Add(MemberRegionClassifier.AllowedRegionsKey, string.Join(",", allowedRegions))
				.Add(MemberRegionClassifier.RegionOrderKey, Helper.ForAdditionalFiles.GetValueFromEditorConfig(Rule.Id, MemberRegionClassifier.RegionOrderKey));
			context.RegisterSyntaxNodeAction(c => Analyze(c, regionNames, allowedRegions), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
		}

		private void Analyze(SyntaxNodeAnalysisContext context, ImmutableDictionary<string, string> regionNames, HashSet<string> allowedRegions)
		{
			var type = (TypeDeclarationSyntax)context.Node;
			if (type.Members.Count == 0 || type.Parent is TypeDeclarationSyntax)
			{
				return;
			}

			if (!Classifier.TryGetCurrentRegions(type, out List<IReadOnlyCollection<string>> currentRegions))
			{
				return;
			}

			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var isTestClass = Classifier.IsTestClass(type);
			for (var i = 0; i < type.Members.Count; i++)
			{
				if (currentRegions[i].Any(allowedRegions.Contains))
				{
					continue;
				}

				MemberDeclarationSyntax member = type.Members[i];
				var expected = Classifier.GetRegionName(Classifier.GetExpectedRegion(member, isTestClass), regionNames);
				if (!Contains(currentRegions[i], expected))
				{
					var memberName = Classifier.GetMemberName(member);
					context.ReportDiagnostic(Diagnostic.Create(Rule, type.Identifier.GetLocation(), regionNames, type.Identifier.Text, memberName, expected));
					return;
				}
			}
		}

		private static bool Contains(IReadOnlyCollection<string> regions, string expected)
		{
			foreach (var region in regions)
			{
				if (region == expected)
				{
					return true;
				}
			}
			return false;
		}
	}
}
