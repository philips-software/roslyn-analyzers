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
	public class AvoidThreadSleepTest : DiagnosticVerifier
	{

		[DataTestMethod]
		[DataRow(@"Thread.Sleep(200);", 5)]
		public void ThreadSleepNotAvoidedTest(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
[TestClass]
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
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidThreadSleep),
				Message = new Regex(AvoidThreadSleepAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 9, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}


		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidThreadSleepAnalyzer();
		}
	}
}