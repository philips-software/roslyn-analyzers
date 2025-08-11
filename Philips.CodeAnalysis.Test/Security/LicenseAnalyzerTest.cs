// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.SecurityAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Security
{
	[TestClass]
	public class LicenseAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LicenseAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerSuccessfulCompilationNoErrorAsync()
		{
			const string testCode = @"
class Foo 
{
  public void DoSomething()
  {
    var x = 1;
  }
}
";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithAdditionalFileCustomLicensesAllowedAsync()
		{
			const string testCode = @"
class Foo 
{
  public void DoSomething()
  {
    var x = 1;
  }
}
";
			const string additionalFileContent = @"CustomLicense
ProprietaryLicense
# This is a comment and should be ignored
GPL-3.0
";

			await VerifySuccessfulCompilation(testCode, additionalFileContent, LicenseAnalyzer.AllowedLicensesFileName).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithEmptyAdditionalFileAsync()
		{
			const string testCode = @"
class Foo 
{
  public void DoSomething()
  {
    var x = 1;
  }
}
";
			const string additionalFileContent = @"
# Only comments and empty lines

";

			await VerifySuccessfulCompilation(testCode, additionalFileContent, LicenseAnalyzer.AllowedLicensesFileName).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithMissingAssetsFileAsync()
		{
			// Test that analyzer doesn't crash when project.assets.json is not available
			const string testCode = @"
class Foo 
{
  public void DoSomething()
  {
    var x = 1;
  }
}
";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}
	}
}
