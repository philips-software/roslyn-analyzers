// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	/// <summary>
	/// 
	/// </summary>
	[TestClass]
	public class AvoidMsFakesAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidMsFakesAnalyzer();
		}

		private string CreateFunction(string content)
		{
			string baseline = @"
class Foo 
{{
  public void Foo()
  {{
    {0};
  }}
}}
";

			return string.Format(baseline, content);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidMsFakesTestAsync()
		{
			var file = CreateFunction("using (ShimsContext.Create()) {}");
			await VerifyDiagnostic(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidMsFakesNotRelevantTestAsync()
		{
			var file = CreateFunction("using (new MemoryStream()) {}");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}
	}
}
