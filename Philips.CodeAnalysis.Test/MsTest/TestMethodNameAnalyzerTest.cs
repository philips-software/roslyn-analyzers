// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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
	public class TestMethodNameAnalyzerTest : CodeFixVerifier
	{
		[DataTestMethod]
		[DataRow("DataTestMethod", "TestSomething", true)]
		[DataRow("DataTestMethod", "SomeTest", false)]
		[DataRow("STATestMethod", "TestSomething", true)]
		[DataRow("STATestMethod", "SomeTest", false)]
		[DataRow("TestMethod", "TestSomething", true)]
		[DataRow("TestMethod", "SomeTest", false)]
		[DataRow("TestMethod", "CheckSomething", false)]
		[DataRow("TestMethod", "SomeCheck", false)]
		[DataRow("TestMethod", "EnsureSomething", true)]
		[DataRow("TestMethod", "SomethingToEnsure", false)]
		[DataRow("TestMethod", "VerifySomething", true)]
		[DataRow("TestMethod", "SomethingToVerify", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AreEqualTypesMatchTest(string testMethodType, string name, bool isError)
		{
			var baseline = @"
namespace TestMethodNameAnalyzerTest
{{
  public class TestClass
  {{
    [{0}]
    public void {1}()
    {{
    }}
  }}
}}
";
			var givenText = string.Format(baseline, testMethodType, name);
			var prefix = GetPrefix(name);
			var expectedMessage = string.Format(TestMethodNameAnalyzer.MessageFormat, prefix);
			var fixedName = FixName(name);
			var fixedText = string.Format(baseline, testMethodType, fixedName);

			if (isError)
			{
				var expected = new DiagnosticResult
				{
					Id = DiagnosticId.TestMethodName.ToId(),
					Message = new Regex(expectedMessage),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", 7, 17)
					}
				};
				await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}

			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestMethodNameAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new TestMethodNameCodeFixProvider();
		}

		private string GetPrefix(string name)
		{
			var prefix = "";
			if (name.StartsWith("Test", StringComparison.OrdinalIgnoreCase))
			{
				prefix = "Test";
			}
			else if (name.StartsWith("Verify", StringComparison.OrdinalIgnoreCase))
			{
				prefix = "Verify";
			}
			if (name.StartsWith("Ensure", StringComparison.OrdinalIgnoreCase))
			{
				prefix = "Ensure";
			}

			return prefix;
		}

		private string FixName(string name)
		{
			var prefix = GetPrefix(name);
			return string.IsNullOrEmpty(prefix) ? name : name.Replace(prefix, "") + "Test";
		}
	}
}
