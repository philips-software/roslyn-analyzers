// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidPragmaAnalyzerTest : DiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow(@"#pragma warning disable 100", 5)]
		public void PragmaWarningNotAvoidedTest(string test, int expectedColumn)
		{
			string baseline = @"
class Foo 
{{
  public void Foo()
  {{
    {0}
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidPragma),
				Message = new Regex(AvoidPragmaAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test.cs", 6, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}

		[TestMethod]
		public void PragmaAllowedDisableSelf()
		{
			string text = @"
class Foo 
{{
  #pragma warning disable PH2029
  public void Foo()
  {{
    return;
  }}
}}
";
			VerifyCSharpDiagnostic(text);
		}

		[DataTestMethod]
		[DataRow(@"#pragma warning disable 100", 5)]
		public void PragmaAllowedGeneratedCode(string test, int expectedColumn)
		{
			string baseline = @"
class Foo 
{{
  public void Foo()
  {{
    {0}
  }}
}}
";
			string givenText = string.Format(baseline, test);

			VerifyCSharpDiagnostic(givenText, "Test.Designer");
		}


		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidPragmaAnalyzer();
		}
	}
}