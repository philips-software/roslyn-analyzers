// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestMethodNameAnalyzerTest : DiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow("Test1", true)]
		[DataRow("SomeTest", false)]
		[DataRow("Check1", false)]
		[DataRow("SomeCheck", false)]
		[DataRow("Ensure1", true)]
		[DataRow("SomethingToEnsure", false)]
		[DataRow("Verify1", true)]
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
			string expectedMessage = string.Format(TestMethodNameAnalyzer.MessageFormat, GetPrefix(name));

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

			VerifyCSharpDiagnostic(givenText, "Test0", (isError) ? expected : Array.Empty<DiagnosticResult>());
		}
		
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TestMethodNameAnalyzer();
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
	}
}