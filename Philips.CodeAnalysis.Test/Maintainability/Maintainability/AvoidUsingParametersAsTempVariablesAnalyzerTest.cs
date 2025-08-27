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

		private const string CorrectNoParameters = @"
namespace AvoidUsingParametersTest {
    public class Number {
        private void MethodA() {
            int b = 5;
        }
    }
}";

		private const string CorrectOutParameter = @"
namespace AvoidUsingParametersTest {
    public class Number {
        private void MethodA(out int a) {
            a = 5;
        }
    }
}";

		private const string CorrectRefParameter = @"
namespace AvoidUsingParametersTest {
    public class Number {
        private void MethodA(ref int a) {
            a = 5;
        }
    }
}";

		private const string CorrectStaticField = @"
namespace AvoidUsingParametersTest {
    public class Number {
        private static int s;
        private void MethodA(int a) {
            Number.s = 5;
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
		 DataRow(Correct, DisplayName = nameof(Correct)),
		 DataRow(CorrectNoParameters, DisplayName = nameof(CorrectNoParameters)),
		 DataRow(CorrectOutParameter, DisplayName = nameof(CorrectOutParameter)),
		 DataRow(CorrectRefParameter, DisplayName = nameof(CorrectRefParameter)),
		 DataRow(CorrectStaticField, DisplayName = nameof(CorrectStaticField))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(Wrong, DisplayName = nameof(Wrong))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenMismatchOfPlusMinusDiagnosticIsRaisedAsync(string testCode)
		{
			await VerifyDiagnostic(testCode, DiagnosticId.AvoidUsingParametersAsTempVariables).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
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
