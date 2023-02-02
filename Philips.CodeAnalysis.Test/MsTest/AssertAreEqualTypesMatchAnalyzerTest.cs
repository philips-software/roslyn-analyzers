// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
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
		[TestCategory(TestDefinitions.UnitTests)]
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
			var arg1Type = GetWellKnownTypeName(arg1);
			var arg2Type = GetWellKnownTypeName(arg2);
			string expectedMessage = string.Format(AssertAreEqualTypesMatchAnalyzer.MessageFormat, arg1Type, arg2Type);

			DiagnosticResult[] expected = new [] { new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AssertAreEqualTypesMatch),
				Message = new Regex(expectedMessage),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 15, 7)
				}
			}};

			VerifyDiagnostic(givenText, "Test0", isError ? expected : Array.Empty<DiagnosticResult>());
		}
		
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AssertAreEqualTypesMatchAnalyzer();
		}

		private string GetWellKnownTypeName(string variableName)
		{
			string typeName = variableName switch
			{
				"i" or "j" => "int",
				"str1" or "str2" => "string",
				"f1" or "f2" => "float",
				"d1" or "d2" => "double",
				"b1" or "b2" => "bool",
				"x1" or "x2" => "byte",
				_ => "",
			};
			return typeName;
		}
	}
}