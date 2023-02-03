// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidPragmaAnalyzerTest : DiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow(@"#pragma warning disable 100", 5)]
		[TestCategory(TestDefinitions.UnitTests)]
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

			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AvoidPragma),
				Message = new Regex(AvoidPragmaAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test.cs", 6, expectedColumn)
				}
			};

			VerifyDiagnostic(givenText, expected);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifySuccessfulCompilation(text);
		}

		[DataTestMethod]
		[DataRow(@"#pragma warning disable 100", 5)]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifySuccessfulCompilation(givenText, "Test.Designer");
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidPragmaAnalyzer();
		}
	}
}