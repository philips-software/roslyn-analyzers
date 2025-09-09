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
	/// Test class for <see cref="AlignOperatorsCountAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AlignOperatorsCountAnalyzerTest : DiagnosticVerifier
	{
		private const string CorrectNumberOfIncrementDecrement = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static Number operator ++(Number num1)
            {
                return num1.n + 1;
            }
            public static Number operator --(Number num1)
            {
                return num1.n - num2.n;
            }
        }
    }";

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
            public static Number operator ==(Number num1, Number num2)
            {
                return num1.n == num2.n;
            }
        }
    }";

		private const string CorrectNumberOfPlusMinusOnStruct = @"
    namespace AssignmentInConditionUnitTests {
        public struct Number {
			private int n;
            public static Number operator +(Number num1, Number num2)
            {
                return num1.n + num2.n;
            }
            public static Number operator -(Number num1, Number num2)
            {
                return num1.n - num2.n;
            }
            public static Number operator ==(Number num1, Number num2)
            {
                return num1.n == num2.n;
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
		private const string CorrectNumberOfGreaterLessThan = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static bool operator >(Number num1, Number num2)
            {
                return num1.n > num2.n;
            }
            public static bool operator <(Number num1, Number num2)
            {
                return num1.n < num2.n;
            }
        }
    }";
		private const string CorrectNumberOfGreaterLessThanOrEqual = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static bool operator >=(Number num1, Number num2)
            {
                return num1.n > num2.n;
            }
            public static bool operator <=(Number num1, Number num2)
            {
                return num1.n < num2.n;
            }
        }
    }";
		private const string CorrectNumberOfRightLeftShift = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static Number operator >>(Number num1, int i)
            {
                return num1.n >> i;
            }
            public static Number operator <<(Number num1, int i)
            {
                return num1.n << i;
            }
        }
    }";

		private const string CorrectOnlyEqual = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static Number operator ==(Number num1, Number num2)
            {
                return num1.n == num2.n;
            }
            public static Number operator !=(Number num1, Number num2)
            {
                return num1.n != num2.n;
            }
        }
    }";

		private const string WrongNumberOfIncrementDecrement = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static Number operator ++(Number num1)
            {
                return num1.n + 1;
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
            public static Number operator ==(Number num1, Number num2)
            {
                return num1.n == num2.n;
            }
        }
    }";

		private const string WrongNumberOfPlusMinusOnStruct = @"
    namespace AssignmentInConditionUnitTests {
        public struct Number {
			private int n;
            public static Number operator +(Number num1, Number num2)
            {
                return num1.n + num2.n;
            }
             public static Number operator ==(Number num1, Number num2)
            {
                return num1.n == num2.n;
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
		private const string WrongNumberOfGreaterLessThan = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static bool operator <(Number num1, Number num2)
            {
                return num1.n < num2.n;
            }
        }
    }";
		private const string WrongNumberOfGreaterLessThanOrEqual = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static bool operator <=(Number num1, Number num2)
            {
                return num1.n < num2.n;
            }
        }
    }";
		private const string WrongNumberOfRightLeftShift = @"
    namespace AssignmentInConditionUnitTests {
        public class Number {
			private int n;
            public static Number operator >>(Number num1, int i)
            {
                return num1.n >> i;
            }
            public static Number operator >>(Number num1, int i)
            {
                return num1.n >> i;
            }
        }
    }";
		private const string WrongNumberOfPlusEqual = @"
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

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectNumberOfIncrementDecrement, DisplayName = nameof(CorrectNumberOfIncrementDecrement)),
		 DataRow(CorrectNumberOfPlusMinus, DisplayName = nameof(CorrectNumberOfPlusMinus)),
		 DataRow(CorrectNumberOfPlusMinusOnStruct, DisplayName = nameof(CorrectNumberOfPlusMinusOnStruct)),
		 DataRow(CorrectNumberOfMultiplyDivide, DisplayName = nameof(CorrectNumberOfMultiplyDivide)),
		 DataRow(CorrectNumberOfGreaterLessThan, DisplayName = nameof(CorrectNumberOfGreaterLessThan)),
		 DataRow(CorrectNumberOfGreaterLessThanOrEqual, DisplayName = nameof(CorrectNumberOfGreaterLessThanOrEqual)),
		 DataRow(CorrectNumberOfRightLeftShift, DisplayName = nameof(CorrectNumberOfRightLeftShift)),
		 DataRow(CorrectOnlyEqual, DisplayName = nameof(CorrectOnlyEqual))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(WrongNumberOfIncrementDecrement, DiagnosticId.AlignNumberOfIncrementAndDecrementOperators, DisplayName = nameof(WrongNumberOfIncrementDecrement)),
		 DataRow(WrongNumberOfPlusMinus, DiagnosticId.AlignNumberOfPlusAndMinusOperators, DisplayName = nameof(WrongNumberOfPlusMinus)),
		 DataRow(WrongNumberOfPlusMinusOnStruct, DiagnosticId.AlignNumberOfPlusAndMinusOperators, DisplayName = nameof(WrongNumberOfPlusMinusOnStruct)),
		 DataRow(WrongNumberOfMultiplyDivide, DiagnosticId.AlignNumberOfMultiplyAndDivideOperators, DisplayName = nameof(WrongNumberOfMultiplyDivide)),
		 DataRow(WrongNumberOfGreaterLessThan, DiagnosticId.AlignNumberOfGreaterAndLessThanOperators, DisplayName = nameof(WrongNumberOfGreaterLessThan)),
		 DataRow(WrongNumberOfGreaterLessThanOrEqual, DiagnosticId.AlignNumberOfGreaterAndLessThanOrEqualOperators, DisplayName = nameof(WrongNumberOfGreaterLessThanOrEqual)),
		 DataRow(WrongNumberOfRightLeftShift, DiagnosticId.AlignNumberOfShiftRightAndLeftOperators, DisplayName = nameof(WrongNumberOfRightLeftShift)),
		 DataRow(WrongNumberOfPlusEqual, DiagnosticId.AlignNumberOfPlusAndEqualOperators, DisplayName = nameof(WrongNumberOfPlusEqual))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenMismatchOfPlusMinusDiagnosticIsRaisedAsync(string testCode, DiagnosticId diagnosticId)
		{
			await VerifyDiagnostic(testCode, diagnosticId).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow("File.g")]
		[DataRow("GlobalSuppressions")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggeredAsync(string filePath)
		{
			await VerifySuccessfulCompilation(WrongNumberOfPlusMinus, filePath).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AlignOperatorsCountAnalyzer();
		}
	}
}
