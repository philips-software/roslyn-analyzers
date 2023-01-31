// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
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
		[DataRow("TestSomething", true)]
		[DataRow("SomeTest", false)]
		[DataRow("CheckSomething", false)]
		[DataRow("SomeCheck", false)]
		[DataRow("EnsureSomething", true)]
		[DataRow("SomethingToEnsure", false)]
		[DataRow("VerifySomething", true)]
		[DataRow("SomethingToVerify", false)]
		public void AreEqualTypesMatchTest(string name, bool isError)
		{
			string baseline = @"
namespace TestMethodNameAnalyzerTest
{{
  public class TestClass
  {{
    [TestMethod]
    public void {0}()
    {{
    }}
  }}
}}
";

			string givenText = string.Format(baseline, name);
			var prefix = GetPrefix(name);
			string expectedMessage = string.Format(TestMethodNameAnalyzer.MessageFormat, prefix);
			var fixedName = FixName(name);
			string fixedText = string.Format(baseline, fixedName);

			DiagnosticResult[] expected = new [] { new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.TestMethodName),
				Message = new Regex(expectedMessage),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 7, 17)
				}
			}};

			VerifyDiagnostic(givenText, "Test0", isError ? expected : Array.Empty<DiagnosticResult>());
			VerifyFix(givenText, fixedText);
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
			string prefix = "";
			if (name.StartsWith("Test", StringComparison.OrdinalIgnoreCase))
			{
				prefix = "Test";
			} else if(name.StartsWith("Verify", StringComparison.OrdinalIgnoreCase))
			{
				prefix = "Verify";
			}
			if(name.StartsWith("Ensure", StringComparison.OrdinalIgnoreCase))
			{
				prefix = "Ensure";
			}

			return prefix;
		}

		private string FixName(string name)
		{
			string prefix = GetPrefix(name);
			return string.IsNullOrEmpty(prefix) ? name : name.Replace(prefix, "") + "Test";
		}
	}
}