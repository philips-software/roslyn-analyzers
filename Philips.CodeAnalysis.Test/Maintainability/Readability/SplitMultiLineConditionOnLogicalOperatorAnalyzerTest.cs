// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class SplitMultiLineConditionOnLogicalOperatorAnalyzerTest : CodeFixVerifier
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

		private const string CorrectStartOnSameLine = @"
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

		private const string CorrectFromCommon = @"
public class Foo {
	public static bool IsExtensionClass(INamedTypeSymbol declaredSymbol)
	{
		return 
			declaredSymbol is { MightContainExtensionMethods: true } &&
				!declaredSymbol.GetMembers().Any(m =>
					m.Kind == SymbolKind.Method &&
					m.DeclaredAccessibility == Accessibility.Public &&
					!((IMethodSymbol)m).IsExtensionMethod);
	}
}
";

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
                    2 == 2)
            ) {
                // Blah
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

		private const string WrongReturnStatement = @"
using System;

namespace MultiLineConditionUnitTests
{
    public class Program
    {
        public static bool Main(string[] args)
        {
            return (
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
		 DataRow(CorrectStartOnSameLine, DisplayName = nameof(CorrectStartOnSameLine)),
		 DataRow(CorrectClose, DisplayName = nameof(CorrectClose)),
		 DataRow(SingleLine, DisplayName = nameof(SingleLine)),
		 DataRow(SingleLineWithCondition, DisplayName = nameof(SingleLineWithCondition)),
		 DataRow(CorrectAssignmentToBool, DisplayName = nameof(CorrectAssignmentToBool)),
		 DataRow(CorrectReturnStatement, DisplayName = nameof(CorrectReturnStatement)),
		 DataRow(CorrectMultiLine, DisplayName = nameof(CorrectMultiLine)),
		 DataRow(CorrectFromCommon, DisplayName = nameof(CorrectFromCommon))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongBreak, null, 11, 22, DisplayName = nameof(WrongBreak))]
		[DataRow(WrongMultiLine, CorrectMultiLine, 13, 26, DisplayName = nameof(WrongMultiLine))]
		[DataRow(WrongAssignmentToBool, CorrectAssignmentToBool, 11, 22, DisplayName = nameof(WrongAssignmentToBool))]
		[DataRow(WrongLastTokenDot, null, 11, 18, DisplayName = nameof(WrongLastTokenDot))]
		[DataRow(WrongReturnStatement, CorrectReturnStatement, 11, 22, DisplayName = nameof(WrongReturnStatement))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenMultiLineConditionIsIncorrectDiagnosticIsTriggered(
			string testCode,
			string fixedCode,
			int line,
			int column
		)
		{
			var expected = new DiagnosticResult
			{
				Id = DiagnosticId.SplitMultiLineConditionOnLogicalOperator.ToId(),
				Severity = DiagnosticSeverity.Warning,
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", line, column)
					}
			};
			await VerifyDiagnostic(testCode, expected).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(fixedCode))
			{
				await VerifyFix(testCode, fixedCode).ConfigureAwait(false);
			}
		}


		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Wrong4Violations, 3, DisplayName = nameof(Wrong4Violations))
		]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenMultiLineConditionIsIncorrectInMorePlacesCorrectNumberOfDiagnosticIsTriggeredAsync(
			string testCode,
			int expectedCount
		)
		{
			var expected = new DiagnosticResult
			{
				Id = DiagnosticId.SplitMultiLineConditionOnLogicalOperator.ToId(),
				Severity = DiagnosticSeverity.Warning,
				Location = new DiagnosticResultLocation()
			};
			var expectedArray = new DiagnosticResult[expectedCount];
			Array.Fill(expectedArray, expected);
			await VerifyDiagnostic(testCode, expectedArray).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongBreak, "GlobalSuppressions", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggeredAsync(string testCode, string filePath)
		{
			await VerifySuccessfulCompilation(testCode, filePath).ConfigureAwait(false);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new SplitMultiLineConditionOnLogicalOperatorAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new SplitMultiLineConditionOnLogicalOperatorCodeFixProvider();
		}
	}
}
