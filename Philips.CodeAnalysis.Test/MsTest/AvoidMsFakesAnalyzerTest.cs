// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	/// <summary>
	/// 
	/// </summary>
	[TestClass]
	public class AvoidMsFakesAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

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

		#endregion

		#region Public Interface

		[TestMethod]
		public void AvoidMsFakesTest()
		{
			var file = CreateFunction("using (ShimsContext.Create()) {}");
			VerifyDiagnostic(file);
		}

		[TestMethod]
		public void AvoidMsFakesNotRelevantTest()
		{
			var file = CreateFunction("using (new MemoryStream()) {}");
			VerifyNoDiagnostic(file);
		}


		private void VerifyNoDiagnostic(string file)
		{
			base.VerifyDiagnostic(file);
		}

		private void VerifyDiagnostic(string file)
		{
			VerifyDiagnostic(file, new DiagnosticResult()
			{
				Id = AvoidMsFakesAnalyzer.Rule.Id,
				Message = new Regex(".+"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 6, -1),
				}
			});
		}

		#endregion
	}
}
