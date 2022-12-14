// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class ExpectedExceptionAttributeAnalyzerTest : DiagnosticVerifier
	{
		[TestMethod]
		public void ExpectedExceptionAttributeTest()
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
			DiagnosticResult expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.ExpectedExceptionAttribute),
				Message = new Regex(ExpectedExceptionAttributeAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 6, 6)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}
		
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new ExpectedExceptionAttributeAnalyzer();
		}
	}
}