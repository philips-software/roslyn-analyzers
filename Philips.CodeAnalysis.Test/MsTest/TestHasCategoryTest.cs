using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestHasCategoryTest : CodeFixVerifier
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

			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.TestHasCategoryAttribute),
				Message = new Regex(TestHasCategoryAttributeAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 6, expectedColumn)
				}
			};

			VerifyDiagnostic(givenText, expected);
		}

		[DataTestMethod]
		[DataRow(@"UnitTest", false)]
		[DataRow(@"ManualTest", false)]
		[DataRow(@"NightlyTest", true)]
		[DataRow(@"", true)]
		public void TestHasCategoryAttributeTest2(string category, bool isError)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  [TestMethod, TestCategory(""{0}""]
  public void Foo()
  {{
  }}
}}
";
			VerifyError(baseline, category, isError);
		}

		[DataTestMethod]
		[DataRow(@"UnitTests", false)]
		[DataRow(@"ManualTests", false)]
		[DataRow(@"NightlyTest", true)]
		[DataRow(@"", true)]
		public void TestHasCategoryAttributeIndirectionTest(string category, bool isError)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class TestDefinitions
{{
  public const string {0} = ""blah"";
}}

class Foo
{{
  [TestMethod, TestCategory(TestDefinitions.{0})]
  public void Foo()
  {{
  }}
}}
";
			VerifyError(baseline, category, isError);
		}


		[DataTestMethod]
		[DataRow(@"Foo1", false)]
		[DataRow(@"Foo2", true)]
		public void TestHasCategoryAttributeWhiteListTest(string testName, bool isError)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  [TestMethod, TestCategory(""blah"")]
  public void {0}()
  {{
  }}
}}
";
			VerifyError(baseline, testName, isError);
		}

		[TestMethod]
		public void FixAddsCategoryAttributeTest()
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class TestDefinitions {
    public const string UnitTests = ""UnitTests"";
}
class Foo 
{
    [TestMethod]
    public void TestMethod1()
    {
    }
}
";
			string fixedText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class TestDefinitions {
    public const string UnitTests = ""UnitTests"";
}
class Foo 
{
    [TestMethod]
    [TestCategory(TestDefinitions.UnitTests)]
    public void TestMethod1()
    {
    }
}
";
			VerifyFix(baseline, fixedText);
		}

		private void VerifyError(string baseline, string given, bool isError)
		{
			string givenText = string.Format(baseline, given);
			DiagnosticResult[] results;
			if (isError)
			{
				results = new[] { new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticIds.TestHasCategoryAttribute),
						Message = new System.Text.RegularExpressions.Regex(TestHasCategoryAttributeAnalyzer.MessageFormat),
						Severity = DiagnosticSeverity.Error,
						Locations = new[]
						{
							new DiagnosticResultLocation("Test0.cs", null, null)
						}
					}
				};
			}
			else
			{
				results = Array.Empty<DiagnosticResult>();
			}
			VerifyDiagnostic(givenText, results);
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestHasCategoryAttributeAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new TestHasCategoryCodeFixProvider();
		}

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new[] { (@"TestsWithUnsupportedCategory.Allowed.txt", "Foo.Foo1") };
		}

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			var options = new Dictionary<string, string>
			{
				{ $@"dotnet_code_quality.{Helper.ToDiagnosticId(DiagnosticIds.TestHasCategoryAttribute)}.allowed_test_categories", @"""UnitTest"",""ManualTest"",TestDefinitions.UnitTests,TestDefinitions.ManualTests" }
			};
			return options;
		}
	}
}