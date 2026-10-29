// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class SortMembersIntoRegionsConfiguredNamesTest : CodeFixVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ConfiguredRegionNameIsAccepted()
		{
			var givenText = @"
class Foo
{
	#region Private Interface
	private void M() { }
	#endregion
}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DefaultRegionNameIsFlaggedWhenRenamed()
		{
			var givenText = @"
class Foo
{
	#region Non-Public Properties/Methods
	private void M() { }
	#endregion
}
";
			await VerifyDiagnostic(givenText, regex: "Private Interface").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FixUsesConfiguredRegionNames()
		{
			var givenText = @"
class Foo
{
	public void A() { }
	private int _b;
	private void C() { }
}
";
			var fixedText = @"
class Foo
{
	#region Private Data

	private int _b;

	#endregion

	#region Private Interface

	private void C() { }

	#endregion

	#region Public Interface

	public void A() { }

	#endregion
}
";
			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
		}

		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			var prefix = $@"dotnet_code_quality.{DiagnosticId.SortMembersIntoRegions.ToId()}";
			return base.GetAdditionalAnalyzerConfigOptions()
				.Add($@"{prefix}.non_public_data_members_region", @"Private Data")
				.Add($@"{prefix}.non_public_properties_methods_region", @" Private Interface ");
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new SortMembersIntoRegionsAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new SortMembersIntoRegionsCodeFixProvider();
		}
	}
}
