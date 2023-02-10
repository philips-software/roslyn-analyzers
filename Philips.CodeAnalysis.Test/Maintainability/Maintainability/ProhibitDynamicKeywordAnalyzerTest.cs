// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class ProhibitDynamicKeywordAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new ProhibitDynamicKeywordAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataRow(@"void TestMethod() { dynamic i = 5; }")]
		[DataRow(@"dynamic TestMethod() { return 5; }")]
		[DataRow(@"void TestMethod(dynamic i) { return 5; }")]
		[DataRow(@"void TestMethod() { List<dynamic> list = null; }")]
		[DataRow(@"void TestMethod() { var t = (dynamic)4; }")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DynamicTypeShouldTriggerDiagnostic(string testCode)
		{
			VerifyDiagnostic(testCode);
		}

		[DataRow(@"void TestMethod() { string dynamic = ""test""; }")]
		[DataRow(@"void TestMethod() { string dynamic = mrModule.DynamicSeries;
bool isDynamic = !String.IsNullOrEmpty(dynamic) &&
dynamic.StartsWith(""Y"", true, CultureInfo.CurrentCulture);
 }")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CantBeDynamicAsync(string testCode, int errorCount)
		{
			if (errorCount == 0)
			{
				await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
			}
			else if (errorCount == 1)
			{
				await VerifyDiagnostic(testCode).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(testCode, errorCount).ConfigureAwait(false);
			}
		}

		#endregion
	}
}
