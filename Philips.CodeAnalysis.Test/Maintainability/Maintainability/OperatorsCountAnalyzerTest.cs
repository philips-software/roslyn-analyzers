// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="OperatorsCountAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class OperatorsCountAnalyzerTest : DiagnosticVerifier
	{
		private const string CorrectNumberOfPlusMinus = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static Number operator +(Number num1, Number num2)
            {
                return num1.n + num2.n;
            }
            public static Number operator -(Number num1, Number num2)
            {
                return num1.n - num2.n;
            }
        }
    }";

		private const string CorrectNumberOfMultiplyDivide = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static Number operator *(Number num1, Number num2)
            {
                return num1.n * num2.n;
            }
            public static Number operator /(Number num1, Number num2)
            {
                return num1.n / num2.n;
            }
        }
    }";

		private const string WrongNumberOfPlusMinus = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static Number operator +(Number num1, Number num2)
            {
                return num1.n + num2.n;
            }
        }
    }";

		private const string WrongNumberOfMultiplyDivide = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static Number operator *(Number num1, Number num2)
            {
                return num1.n * num2.n;
            }
        }
    }";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectNumberOfPlusMinus, DisplayName = nameof(CorrectNumberOfPlusMinus)),
		 DataRow(CorrectNumberOfMultiplyDivide, DisplayName = nameof(CorrectNumberOfMultiplyDivide))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(WrongNumberOfPlusMinus, DiagnosticIds.AlignNumberOfPlusAndMinusOperators , DisplayName = nameof(WrongNumberOfPlusMinus)),
		 DataRow(WrongNumberOfMultiplyDivide, DiagnosticIds.AlignNumberOfMultiplyAndDivideOperators, DisplayName = nameof(WrongNumberOfMultiplyDivide))]
		public void WhenMismatchOfPlusMinusDiagnosticIsRaised(string testCode, DiagnosticIds diagnosticId) {
			var expected = DiagnosticResultHelper.Create(diagnosticId);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifyCSharpDiagnostic(WrongNumberOfPlusMinus, filePath);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new OperatorsCountAnalyzer();
		}
	}
}
