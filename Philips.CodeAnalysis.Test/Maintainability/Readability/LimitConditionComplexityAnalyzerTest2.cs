// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	/// <summary>
	/// Test class for <see cref="LimitConditionComplexityAnalyzer"/>, testing wrong.editorconfig usage results in default value
	/// </summary>
	[TestClass]
	public class LimitConditionComplexityAnalyzerTest2 : DiagnosticVerifier
	{
		private const string IncorrectCode = @"
using System;

namespace ComplexConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (2 == 3 && (4 == 5 || 9 == 8) && 6 == 7 && 4 == 5) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";
		private const string CorrectCode = @"
using System;

namespace ComplexConditionUnitTests {
    public class Program {
        public static void Main(string[] args) {
            if (2 == 3 && (4 == 5 || 9 == 8) && 6 == 7) {
                Console.WriteLine('Hello world!');
            }
        }
    }
}";

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(CorrectCode, DisplayName = nameof(WhenConfigInvalidDefaultValueUsedAndCorrectCodePasses))]
		public void WhenConfigInvalidDefaultValueUsedAndCorrectCodePasses(string testCode)
		{
			VerifyDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(IncorrectCode, DisplayName = nameof(WhenConfigInvalidDefaultValueUsedAndIncorrectCodeFails))]
		public void WhenConfigInvalidDefaultValueUsedAndIncorrectCodeFails(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.LimitConditionComplexity);
			VerifyDiagnostic(testCode, expected);
		}


		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LimitConditionComplexityAnalyzer();
		}

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			var key =
				$@"dotnet_code_quality.{Helper.ToDiagnosticId(DiagnosticIds.LimitConditionComplexity)}.max_operators";
			Dictionary<string, string> options = new()
			{
				{ key, "not a number" }
			};
			return options;
		}
	}
}
