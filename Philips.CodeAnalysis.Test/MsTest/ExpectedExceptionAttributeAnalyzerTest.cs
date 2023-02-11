// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class ExpectedExceptionAttributeAnalyzerTest : DiagnosticVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExpectedExceptionAttributeTestAsync()
		{
			string givenText = @"
namespace ExpectedAnalyzerAttributeTest
{
  public class TestClass
  {
    [ExpectedException]
    [TestMethod]
    public void TestMethod()
    {
    }
  }
}
";
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new ExpectedExceptionAttributeAnalyzer();
		}
	}
}
