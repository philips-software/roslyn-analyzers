// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class SplitMultiLineConditionOnLogicalOperatorAnalyzerTests : CodeFixVerifier
	{

		private const string Correct = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (
                2 == 3 &&
                4 == 5)
			{
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		private const string CorrectClose = @"
using System;

namespace MultiLineConditionUnitTests 
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (
                2 == 3 &&
                4 == 5)
			{
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		private const string SingleLine = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (2 == 3 && 4 == 5)
			{
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		private const string SingleLineWithCondition = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            (2 == 3 && 4 == 5) ? 
				Console.WriteLine('Hello world!') :
				Console.WriteLine('Goodbye world!');
            }
        }
    }
}";

		private const string WrongBreak = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
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

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
             int i = (
                 3.
                   Equals(3) &&
                 3.Equals(4)) ? 5 : 6;
        }
    }
}";

		private const string CorrectMultiLine = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (
				3 == 3 && 
                5 == 6 && ( 
                    1 == 1 || 
                    2 == 2) ||
				8 == 4)
			{
            }
        }
    }
}";

		private const string WrongMultiLine = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (
				3 == 3 && 
                5 == 6 && ( 
                    1 == 1 
                    || 2 == 2)
            ) {
                // Blah
            }
        }
    }
}";

		        private const string WrongOpening = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (2 == 3 &&
                4 == 5)
            {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		        private const string CorrectAssignmentToBool = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool b = (
                2 == 3 &&
                4 == 5);
        }
    }
}";

		        private const string WrongAssignmentToBool = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool b = (
                2 == 3
                && 4 == 5);
        }
    }
}";

		        private const string CorrectReturnStatement = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static bool Main(string[] args)
        {
            return (
                2 == 3 &&
                4 == 5);
        }
    }
}";

		        private const string WrongReturnStatement = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static bool Main(string[] args)
        {
            return (2 == 3 &&
                4 == 5);
        }
    }
}";

		        private const string Wrong4Violations = @"
namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static bool Main(string[] args)
        {
            if (str.StartsWith(EqualsSign, System.StringComparison.Ordinal)
		    || str.StartsWith(MinusSign, System.StringComparison.Ordinal)
		    || str.StartsWith(PlusSign, System.StringComparison.Ordinal)
		    || str.StartsWith(AtSymbol, System.StringComparison.Ordinal))
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
			DataRow(SingleLineWithCondition, DisplayName = nameof(SingleLineWithCondition)),
			DataRow(CorrectAssignmentToBool, DisplayName = nameof(CorrectAssignmentToBool)),
			DataRow(CorrectReturnStatement, DisplayName = nameof(CorrectReturnStatement)),
			DataRow(CorrectMultiLine, DisplayName = nameof(CorrectMultiLine))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongBreak, null, 11, 22, DisplayName = nameof(WrongBreak)),
			DataRow(WrongOpening, 10, 13, DisplayName = nameof(WrongOpening)),
			DataRow(WrongMultiLine, CorrectMultiLine, 13, 26, DisplayName = nameof(WrongMultiLine)),
			DataRow(WrongReturnStatement, CorrectReturnStatement, 10, 20, DisplayName = nameof(WrongReturnStatement)),
			DataRow(WrongAssignmentToBool, CorrectAssignmentToBool, 11, 22, DisplayName = nameof(WrongAssignmentToBool)),
			DataRow(WrongLastTokenDot, null, 11, 18, DisplayName = nameof(WrongLastTokenDot))
		]
		public void WhenMultiLineConditionIsIncorrectDiagnosticIsTriggered(
			string testCode,
			string fixedCode,
			int line,
			int column
		)
		{
			var expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.SplitMultiLineConditionOnLogicalOperator),
				Severity = DiagnosticSeverity.Warning,
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", line, column)
					}
			};
			VerifyCSharpDiagnostic(testCode, expected);
			if (!string.IsNullOrEmpty(fixedCode))
			{
				VerifyCSharpFix(testCode, fixedCode);
			}
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Wrong4Violations, 4, DisplayName = nameof(Wrong4Violations))
		]
		public void WhenMultiLineConditionIsIncorrectInMorePlacesCorrectNumberOfDiagnosticIsTriggered(
			string testCode,
			int expectedCount
		)
		{
			var expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.SplitMultiLineConditionOnLogicalOperator),
				Severity = DiagnosticSeverity.Warning,
				Location = new DiagnosticResultLocation()
			};
			var expectedArray = new DiagnosticResult[expectedCount];
			Array.Fill(expectedArray, expected);
			VerifyCSharpDiagnostic(testCode, expectedArray);
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
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new SplitMultiLineConditionOnLogicalOperatorAnalyzer();
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new SplitMultiLineConditionOnLogicalOperatorCodeFixProvider();
		}
	}
}
