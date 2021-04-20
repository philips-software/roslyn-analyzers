// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	/// <summary>
	/// Test class for <see cref="LimitConditionComplexityAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class LimitConditionComplexityAnalyzerTest : DiagnosticVerifier
	{
		private const int ConfiguredMaxOperators = 3;
		private const string Correct = @"
using System;

namespace ComplexConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (2 == 3 && (4 == 5 || 9 == 8)) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		private const string CorrectSingle = @"
using System;

namespace ComplexConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (2 == 3) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		private const string Wrong = @"
using System;

namespace ComplexConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (3 == 4 && 5 == 6 || (7 == 9 && 8 == 1)) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";


		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(Correct, DisplayName = nameof(Correct)),
			DataRow(CorrectSingle, DisplayName = nameof(CorrectSingle))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(Wrong, DisplayName = nameof(Wrong))]
		public void WhenConditionIsTooComplexDiagnosticIsTriggered(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(Common.DiagnosticIds.LimitConditionComplexity);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow(Wrong, "Dummy.Designer", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyCSharpDiagnostic(testCode, filePath);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new LimitConditionComplexityAnalyzer();
		}

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			var key =
				$@"dotnet_code_quality.{Helper.ToDiagnosticId(DiagnosticIds.LimitConditionComplexity)}.max_operators";
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ key, ConfiguredMaxOperators.ToString() }
			};
			return options;
		}
	}
}
