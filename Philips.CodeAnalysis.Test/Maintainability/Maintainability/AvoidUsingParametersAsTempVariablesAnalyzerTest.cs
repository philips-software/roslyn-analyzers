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
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.AvoidUsingParametersAsTempVariables);
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
