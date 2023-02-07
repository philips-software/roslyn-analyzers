// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
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
			string baseline = @"
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

			string fixedText = @"
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
			string givenText = string.Format(baseline, test);
			VerifyDiagnostic(givenText);
			await VerifyFix(givenText, fixedText, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
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