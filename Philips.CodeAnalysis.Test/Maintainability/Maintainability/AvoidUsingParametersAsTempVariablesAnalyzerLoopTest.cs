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
	/// <summary>
	/// Test class for <see cref="AvoidUsingParametersAsTempVariablesAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidUsingParametersAsTempVariablesAnalyzerLoopTest : DiagnosticVerifier
	{
		private const string Correct = @"
namespace AvoidUsingParametersTest {
    public class Number {
        private void MethodA(int a) {
            for(int i = 0; i < list.Count; i++) {
                int j = i;
            }
        }
    }
}";

		private const string CorrectNoLoopVariable = @"
namespace AvoidUsingParametersTest {
    public class Number {
        private void MethodA(int a) {
            int i = 0;
            for(; i < list.Count; i++) {
                int j = i;
            }
        }
    }
}";

		private const string Wrong = @"
using System.Collections.Generic;
namespace AvoidUsingParametersTest {
    public class Number {
        private void MethodA(List<int> list) {
            for(int i = 0; i < list.Count; i++) {
                i = 4;
            }
        }
    }
}";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(Correct, DisplayName = nameof(Correct)),
		 DataRow(CorrectNoLoopVariable, DisplayName = nameof(CorrectNoLoopVariable))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow(Wrong, DisplayName = nameof(Wrong))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenMismatchOfPlusMinusDiagnosticIsRaisedAsync(string testCode)
		{
			await VerifyDiagnostic(testCode, DiagnosticId.AvoidChangingLoopVariables).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggeredAsync(string filePath)
		{
			await VerifySuccessfulCompilation(Wrong, filePath).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidUsingParametersAsTempVariablesAnalyzer();
		}
	}
}
