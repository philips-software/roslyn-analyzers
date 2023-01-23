// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="AvoidUsingParametersAsTempVariablesAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidUsingParametersAsTempVariablesAnalyzerTest : DiagnosticVerifier
	{
		private const string Correct = @"
namespace AvoidUsingParametersTest {
    public class Number {
        private void MethodA(int a) {
            int b = 5;
        }
    }
}";

		private const string Wrong = @"
namespace AvoidUsingParametersTest {
    public class Number {
        private void MethodA(int a) {
            a = 5;
        }
    }
}";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(Correct, DisplayName = nameof(Correct))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(Wrong, DisplayName = nameof(Wrong))]
		public void WhenMismatchOfPlusMinusDiagnosticIsRaised(string testCode) {
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.AvoidArrayList);
			VerifyDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifyDiagnostic(Wrong, filePath);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
			return new AvoidUsingParametersAsTempVariablesAnalyzer();
		}
	}
}
