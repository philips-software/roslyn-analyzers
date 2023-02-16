// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestHasCategoryAnalyzerTest : CodeFixVerifier
	{
		[DataTestMethod]
		[DataRow(@"[TestMethod, Owner(""MK"")]", 15)]
		[DataRow(@"[Owner(""MK""), TestMethod]", 15)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestHasCategoryAttributeTestAsync(string test, int expectedColumn)
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
				Id = Helper.ToDiagnosticId(DiagnosticId.TestHasCategoryAttribute),
				Message = new Regex(TestHasCategoryAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, expectedColumn) }
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"UnitTest", false)]
		[DataRow(@"ManualTest", false)]
		[DataRow(@"NightlyTest", true)]
		[DataRow(@"", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestHasCategoryAttributeTest2Async(string category, bool isError)
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
			await VerifyErrorAsync(baseline, category, isError).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"UnitTests", false)]
		[DataRow(@"ManualTests", false)]
		[DataRow(@"NightlyTest", true)]
		[DataRow(@"", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestHasCategoryAttributeIndirectionTestAsync(string category, bool isError)
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
			await VerifyErrorAsync(baseline, category, isError).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow(@"Foo1", false)]
		[DataRow(@"Foo2", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestHasCategoryAttributeWhiteListTestAsync(string testName, bool isError)
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
			await VerifyErrorAsync(baseline, testName, isError).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FixAddsCategoryAttributeTest()
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
			await VerifyFix(baseline, fixedText).ConfigureAwait(false);
		}

		private async Task VerifyErrorAsync(string baseline, string given, bool isError)
		{
			string givenText = string.Format(baseline, given);
			if (isError)
			{
				var results =
					new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticId.TestHasCategoryAttribute),
						Message = new Regex(TestHasCategoryAnalyzer.MessageFormat),
						Severity = DiagnosticSeverity.Error,
						Locations = new[] { new DiagnosticResultLocation("Test0.cs", null, null) }
					};
				await VerifyDiagnostic(givenText, results).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestHasCategoryAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new TestHasCategoryCodeFixProvider();
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return base.GetAdditionalTexts().Add((@"TestsWithUnsupportedCategory.Allowed.txt", "*.Foo.Foo1"));
		}
		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			var key = $@"dotnet_code_quality.{Helper.ToDiagnosticId(DiagnosticId.TestHasCategoryAttribute)}.allowed_test_categories";
			var value = @"""UnitTest"",""ManualTest"",TestDefinitions.UnitTests,TestDefinitions.ManualTests";
			return base.GetAdditionalAnalyzerConfigOptions().Add(key, value);
		}
	}
}
