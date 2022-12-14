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
	public class AssertAreEqualTypesMatchAnalyzerTest : DiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow("i", "j", false)]
		[DataRow("i", "str2", true)]
		[DataRow("str1", "str2", false)]
		[DataRow("str1", "b2", false)]
		[DataRow("i", "b2", false)]
		[DataRow("d1", "b2", false)]
		[DataRow("f1", "b2", false)]
		[DataRow("x1", "b2", false)]
		public void AreEqualTypesMatchTest(string arg1, string arg2, bool isError)
		{
			string baseline = @"
namespace AssertAreEqualTypesMatchAnalyzerTest
{{
  public class TestClass
  {{
    [TestMethod]
    public void TestMethod()
    {{
      string str1, str2;
      int i, j;
      double d1, d2;
      float f1, f2;
      byte x1, x2;
      boolean b1, b2;
      Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual({0}, {1});
    }}
  }}
}}
";

			string givenText = string.Format(baseline, arg1, arg2);
			string expectedMessage = string.Format(AssertAreEqualTypesMatchAnalyzer.MessageFormat, GetWellKnownTypeName(arg1), GetWellKnownTypeName(arg2));

			DiagnosticResult[] expected = new [] { new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AssertAreEqualTypesMatch),
				Message = new Regex(expectedMessage),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 15, 7)
				}
			}};

			VerifyCSharpDiagnostic(givenText, "Test0", (isError) ? expected : Array.Empty<DiagnosticResult>());
		}
		
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AssertAreEqualTypesMatchAnalyzer();
		}

		private string GetWellKnownTypeName(string variableName)
		{
			string typeName;
			switch (variableName)
			{
				case "i":
				case "j":
					typeName = "int";
					break;
				case "str1":
				case "str2":
					typeName = "string";
					break;
				case "f1":
				case "f2":
					typeName = "float";
					break;
				case "d1":
				case "d2":
					typeName = "double";
					break;
				case "b1":
				case "b2":
					typeName = "bool";
					break;
				case "x1":
				case "x2":
					typeName = "byte";
					break;
				default:
					typeName = "";
					break;
			}

			return typeName;
		}
	}
}