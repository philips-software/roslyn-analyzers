// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class DisallowDisposeRegistrationCodeFixProviderTest : AssertCodeFixVerifier
	{
		[TestMethod]
		public void DisallowDisposeRegistrationTest()
		{
			string givenText = @"
class Foo 
{{
  public event EventHandler MyEvent;
  public void Dispose(bool isDisposing)
  {{
    MyEvent += MyHandler;
  }}
  public void MyHandler(object sender, EventArgs e) => {{ }}
}}
";
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.DisallowDisposeRegistration),
				Message = new Regex(DisallowDisposeRegistrationAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 7, 5)
				}
			};
			VerifyCSharpDiagnostic(givenText, expected);

			string expectedText = givenText.Replace(@"+=", @"-=");

			VerifyCSharpFix(givenText, expectedText);
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new DisallowDisposeRegistrationCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new DisallowDisposeRegistrationAnalyzer();
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			throw new NotImplementedException();
		}
	}
}
