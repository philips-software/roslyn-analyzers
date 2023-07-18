// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidThreadSleepTest : CodeFixVerifier
	{

		[DataTestMethod]
		[DataRow(@"Thread.Sleep(200);")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ThreadSleepNotAvoidedTest(string test)
		{
			var baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
[TestClass]
class Foo 
{{
  public void Foo()
  {{
    {0}
  }}
}}
";

			var fixedText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
[TestClass]
class Foo 
{
  public void Foo()
  {
  }
}
";
			var givenText = string.Format(baseline, test);
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
			await VerifyFix(givenText, fixedText, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoreLocalFunctions()
		{
			var givenText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
[TestClass]
class Foo 
{{
  public void Foo()
  {{
    void ThreadSleep(int amount)
    {{
    }}
    ThreadSleep(1000);
  }}
}}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidThreadSleepAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidThreadSleepCodeFixProvider();
		}
	}
}
