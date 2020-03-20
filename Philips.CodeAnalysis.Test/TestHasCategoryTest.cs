using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class TestHasCategoryTest : DiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow(@"[TestMethod, Owner(""MK"")]", 15)]
		[DataRow(@"[Owner(""MK""), TestMethod]", 15)]
		public void TestHasCategoryAttributeTest(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.TestHasCategoryAttribute),
				Message = new Regex(TestHasCategoryAttributeAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 6, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}

		[TestMethod]
		public void TestHasCategoryAttributeTest2()
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{
  [TestMethod, TestCategory(TestDefinitions.UnitTests)]
  public void Foo()
  {
  }
}

public static class TestDefinitions
{
	const string UnitTests = @"";
}
";
			VerifyCSharpDiagnostic(baseline);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TestHasCategoryAttributeAnalyzer();
		}
	}
}