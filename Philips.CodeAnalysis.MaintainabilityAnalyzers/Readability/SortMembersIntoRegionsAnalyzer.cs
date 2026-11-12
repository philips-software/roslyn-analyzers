// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Concurrent;
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
			var settingsPerTree = new ConcurrentDictionary<SyntaxTree, TreeSettings>();
			context.RegisterSyntaxNodeAction(
				c => Analyze(c, settingsPerTree.GetOrAdd(c.Node.SyntaxTree, tree => LoadSettings(c.Options, tree))),
				SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
		}

		private TreeSettings LoadSettings(AnalyzerOptions options, SyntaxTree tree)
		{
			AnalyzerConfigOptions config = options.AnalyzerConfigOptionsProvider.GetOptions(tree);
			string Read(string key)
			{
				return config.TryGetValue($@"dotnet_code_quality.{Rule.Id}.{key}", out var value) ? value : string.Empty;
			}

			HashSet<string> allowedRegions = Classifier.ParseAllowedRegions(Read(MemberRegionClassifier.AllowedRegionsKey));
			ImmutableDictionary<string, string> properties = Classifier
				.LoadRegionNames(Read)
				.Add(MemberRegionClassifier.AllowedRegionsKey, string.Join(",", allowedRegions))
				.Add(MemberRegionClassifier.RegionOrderKey, Read(MemberRegionClassifier.RegionOrderKey));
			return new TreeSettings(properties, allowedRegions);
		}

		private void Analyze(SyntaxNodeAnalysisContext context, TreeSettings settings)
		{
			ImmutableDictionary<string, string> regionNames = settings.Properties;
			HashSet<string> allowedRegions = settings.AllowedRegions;
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
				if (!currentRegions[i].Contains(expected))
				{
					var memberName = Classifier.GetMemberName(member);
					context.ReportDiagnostic(Diagnostic.Create(Rule, type.Identifier.GetLocation(), regionNames, type.Identifier.Text, memberName, expected));
					return;
				}
			}
		}

		private sealed class TreeSettings
		{
			public TreeSettings(ImmutableDictionary<string, string> properties, HashSet<string> allowedRegions)
			{
				Properties = properties;
				AllowedRegions = allowedRegions;
			}

			public ImmutableDictionary<string, string> Properties { get; }

			public HashSet<string> AllowedRegions { get; }
		}
	}
}
