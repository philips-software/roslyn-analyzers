using System;
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

		[DataTestMethod]
		[DataRow(@"UnitTest", false)]
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
		[DataRow(@"Foo1", false)]
		[DataRow(@"Foo2", true)]
		public void TestHasCategoryAttributeWhiteListTest(string testName, bool isError)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  [TestMethod, TestCategory(""blah""]
  public void {0}()
  {{
  }}
}}
";
			VerifyError(baseline, testName, isError);
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
							new DiagnosticResultLocation("Test0.cs", 5, 16)
						}
					}
				};
			}
			else
			{
				results = Array.Empty<DiagnosticResult>();
			}
			VerifyCSharpDiagnostic(givenText, results);
		}


		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TestHasCategoryAttributeAnalyzer();
		}

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new[] { (AdditionalFilesHelper.EditorConfig, $"dotnet_code_quality.{Helper.ToDiagnosticId(DiagnosticIds.TestHasCategoryAttribute)}.allowed_test_categories = UnitTest"),
							(@"TestsWithUnsupportedCategory.Allowed.txt", "Foo.Foo1")
			};
		}
	}
}