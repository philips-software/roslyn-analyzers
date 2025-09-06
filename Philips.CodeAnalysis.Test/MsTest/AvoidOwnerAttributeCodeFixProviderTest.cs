// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidOwnerAttributeCodeFixProviderTest : CodeFixVerifier
	{
		[TestMethod]
		[DataRow(@"[TestMethod, Owner(""MK"")]", 16)]
		[DataRow(@"[TestMethod][Owner(""MK"")]", 16)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidOwnerAttributeTest(string test, int expectedColumn)
		{
			var baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo 
{{
  {0}public void Foo()
  {{
  }}
}}
";

			var fixedText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo 
{
  [TestMethod]public void Foo()
  {
  }
}
";

			var givenText = string.Format(baseline, test);

			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.AvoidOwnerAttribute.ToId(),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 6, expectedColumn)
				}
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
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
