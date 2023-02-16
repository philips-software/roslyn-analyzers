// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class EnforceEditorConfigAnalyzerTest1 : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceEditorConfigAnalyzer();
		}

		private const string TestCode = @"
class Foo 
{
}
";

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AbsenceOfEditorConfigShouldTriggerDiagnosticsAsync()
		{
			await VerifyDiagnostic(TestCode, DiagnosticId.EnforceEditorConfig).ConfigureAwait(false);
		}
	}
}
