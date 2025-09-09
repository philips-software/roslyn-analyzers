// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidPragmaAnalyzerTest : DiagnosticVerifier
	{
		[TestMethod]
		[DataRow(@"#pragma warning disable 100")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PragmaWarningNotAvoidedTestAsync(string test)
		{
			var baseline = @"
class Foo 
{{
  public void Foo()
  {{
    {0}
  }}
}}
";
			var givenText = string.Format(baseline, test);
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PragmaAllowedDisableSelfAsync()
		{
			var text = @"
class Foo 
{{
  #pragma warning disable PH2029
  public void Foo()
  {{
    return;
  }}
}}
";
			await VerifySuccessfulCompilation(text).ConfigureAwait(false);
		}

		[TestMethod]
		[DataRow(@"#pragma warning disable 100")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PragmaAllowedGeneratedCodeAsync(string test)
		{
			var baseline = @"
class Foo 
{{
  public void Foo()
  {{
    {0}
  }}
}}
";
			var givenText = string.Format(baseline, test);
			await VerifySuccessfulCompilation(givenText, "Test.Designer").ConfigureAwait(false);
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidPragmaAnalyzer();
		}
	}
}
