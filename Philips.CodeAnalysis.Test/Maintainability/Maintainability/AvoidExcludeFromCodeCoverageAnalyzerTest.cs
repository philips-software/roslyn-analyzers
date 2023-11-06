// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidExcludeFromCodeCoverageAnalyzerTest : DiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow(@"ExcludeFromCodeCoverage")]
		[DataRow(@"ExcludeFromCodeCoverageAttribute")]
		[DataRow(@"System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage")]
		[DataRow(@"CodeCoverageAlias")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExcludeFromCodeCoverageWithFullUsingTest(string test)
		{
			var baseline = @"
using System.Diagnostics.CodeAnalysis;
using CodeCoverageAlias = System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage;
class Foo 
{{
  [{0}]
  public void Foo()
  {{
    return;
  }}
}}
";
			var givenText = string.Format(baseline, test);
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExcludeFromCodeCoverageAbsent()
		{
			var text = @"
class Foo 
{{
  public void Foo()
  {{
    return;
  }}
}}
";
			await VerifySuccessfulCompilation(text).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidExcludeFromCodeCoverageAnalyzer();
		}
	}
}
