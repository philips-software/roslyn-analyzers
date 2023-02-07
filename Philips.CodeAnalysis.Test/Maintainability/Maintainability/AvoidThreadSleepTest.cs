// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
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
		public void ThreadSleepNotAvoidedTest(string test)
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
			VerifyFix(givenText, fixedText, allowNewCompilerDiagnostics: true);
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