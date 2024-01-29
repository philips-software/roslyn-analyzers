// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	[TestClass]
	public class AvoidAssemblyGetEntryAssemblyAnalyzerTest : DiagnosticVerifier
	{

		[DataTestMethod]
		[DataRow(@"Assembly.GetEntryAssembly();")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GetEntryAssemblyNotAvoidedTest(string test)
		{
			var baseline = @"
using System.Reflection;
class Foo 
{{
  public void Foo()
  {{
    var assembly = {0}
  }}
}}
";

			var givenText = string.Format(baseline, test);
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task BehindAlias()
		{
			var givenText = @"
using R = System.Reflection;
class Foo 
{{
  class InsideFoo {{
    public void Foo() {{
      var assembly = R.Assembly.GetEntryAssembly();
    }}
  }}
}}
";
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IncludeTestClasses()
		{
			var givenText = @"
using System.Reflection;
[TestClass]
class Foo 
{{
  public void Foo()
  {{
    var assembly = Assembly.GetEntryAssembly();
  }}
}}
";
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAssemblyGetEntryAssemblyAnalyzer();
		}
	}
}
