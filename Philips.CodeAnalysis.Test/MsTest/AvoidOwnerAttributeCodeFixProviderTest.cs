// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class AvoidOwnerAttributeCodeFixProviderTest : CodeFixVerifier
	{
		[DataTestMethod]
		[DataRow(@"[TestMethod, Owner(""MK"")]", 16)]
		[DataRow(@"[TestMethod][Owner(""MK"")]", 16)]
		public void AvoidOwnerAttributeTest(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo 
{{
  {0}public void Foo()
  {{
  }}
}}
";

			string fixedText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo 
{
  [TestMethod]public void Foo()
  {
  }
}
";
			
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidOwnerAttribute),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 6, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
			VerifyFix(givenText, fixedText);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAttributeAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidOwnerAttributeCodeFixProvider();
		}
	}
}