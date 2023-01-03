// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class AvoidMultipleConditionsOnSameLineAnalyzerTests : DiagnosticVerifier
	{

		private const string Correct = @"
using System;

namespace MultiLineConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (
                2 == 3 &&
                4 == 5) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		private const string CorrectClose = @"
using System;

namespace MultiLineConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (
                2 == 3 &&
                4 == 5) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		private const string SingleLine = @"
using System;

namespace MultiLineConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (2 == 3 && 4 == 5) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		private const string WrongBreak = @"
using System;

namespace MultiLineConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (
                3 == 4
                && 4 == 5) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		private const string WrongLastTokenDot = @"
using System;

namespace MultiLineConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
             if (
                 3.
                   Equals(3) &&
                 3.Equals(4)) {
            }
        }
    }
}";

		private const string CorrectMultiLine = @"
using System;

namespace MultiLineConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (
				3 == 3 && 
                5 == 6 && ( 
                    1 == 1 || 
                    2 == 2)
            ) {
            }
        }
    }
}";

		private const string WrongMultiLine = @"
using System;

namespace MultiLineConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (
				3 == 3 && 
                5 == 6 && ( 
                    1 == 1 
                    || 2 == 2)
            ) {
            }
        }
    }
}";

		        private const string WrongOpening = @"
using System;

namespace MultiLineConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (2 == 3 &&
                4 == 5) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Correct, DisplayName = nameof(Correct)),
			DataRow(CorrectClose, DisplayName = nameof(CorrectClose)),
			DataRow(SingleLine, DisplayName = nameof(SingleLine)),
			DataRow(CorrectMultiLine, DisplayName = nameof(CorrectMultiLine))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongBreak, 8, 22, DisplayName = nameof(WrongBreak)),
			DataRow(WrongOpening, 7, 16, DisplayName = nameof(WrongOpening)),
			DataRow(WrongMultiLine, 10, 26, DisplayName = nameof(WrongMultiLine)),
			DataRow(WrongLastTokenDot, 8, 19, DisplayName = nameof(WrongLastTokenDot))
		]
		public void WhenMultiLineConditionIsIncorrectDiagnosticIsTriggered(
			string testCode,
			int line,
			int column
		)
		{
			var expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidMultipleConditionsOnSameLine),
				Severity = DiagnosticSeverity.Warning,
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", line, column)
					}
			};
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongBreak, "GlobalSuppressions", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyCSharpDiagnostic(testCode, filePath);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
			new AvoidMultipleConditionsOnSameLineAnalyzer();
	}
}
