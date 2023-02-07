// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class DisallowDisposeRegistrationCodeFixProviderTest : CodeFixVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DisallowDisposeRegistrationTest()
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
				Id = Helper.ToDiagnosticId(DiagnosticId.DisallowDisposeRegistration),
				Message = new Regex(DisallowDisposeRegistrationAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 7, 5)
				}
			};
			VerifyDiagnostic(givenText, expected);

			string expectedText = givenText.Replace(@"+=", @"-=");

			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new DisallowDisposeRegistrationCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DisallowDisposeRegistrationAnalyzer();
		}
	}
}
