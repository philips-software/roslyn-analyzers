// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
		[DataRow("i", "d1", false)]
		[DataRow("d1", "i", false)]
		[DataRow("i", "j", false)]
		[DataRow("i", "str2", true)]
		[DataRow("str1", "str2", false)]
		[DataRow("str1", "b2", true)]
		[DataRow("i", "b2", true)]
		[DataRow("d1", "b2", true)]
		[DataRow("f1", "b2", true)]
		[DataRow("x1", "b2", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AreEqualTypesMatchTestAsync(string arg1, string arg2, bool isError)
		{
			var baseline = @"
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
      bool b1, b2;
      Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual({0}, {1});
    }}
  }}
}}
";
			var givenText = string.Format(baseline, arg1, arg2);
			var arg1Type = GetWellKnownTypeName(arg1);
			var arg2Type = GetWellKnownTypeName(arg2);
			var expectedMessage = string.Format(AssertAreEqualTypesMatchAnalyzer.MessageFormat, arg1Type, arg2Type);

			if (isError)
			{
				var expected = new DiagnosticResult
				{
					Id = DiagnosticId.AssertAreEqualTypesMatch.ToId(),
					Message = new Regex(expectedMessage),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", 15, 7)
					}
				};
				await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AssertAreEqualTypesMatchAnalyzer();
		}

		private string GetWellKnownTypeName(string variableName)
		{
			var typeName = variableName switch
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
