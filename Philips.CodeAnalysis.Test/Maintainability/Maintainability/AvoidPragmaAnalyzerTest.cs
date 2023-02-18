// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidPragmaAnalyzerTest : DiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow(@"#pragma warning disable 100")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PragmaWarningNotAvoidedTestAsync(string test)
		{
			string baseline = @"
class Foo 
{{
  public void Foo()
  {{
    {0}
  }}
}}
";
			string givenText = string.Format(baseline, test);
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PragmaAllowedDisableSelfAsync()
		{
			string text = @"
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

		[DataTestMethod]
		[DataRow(@"#pragma warning disable 100")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PragmaAllowedGeneratedCodeAsync(string test)
		{
			string baseline = @"
class Foo 
{{
  public void Foo()
  {{
    {0}
  }}
}}
";
			string givenText = string.Format(baseline, test);
			await VerifySuccessfulCompilation(givenText, "Test.Designer").ConfigureAwait(false);
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidPragmaAnalyzer();
		}
	}
}
