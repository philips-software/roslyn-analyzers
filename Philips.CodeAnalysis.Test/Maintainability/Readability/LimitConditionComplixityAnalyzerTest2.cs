// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	/// <summary>
	/// Test class for <see cref="LimitConditionComplexityAnalyzer"/>, testing wrong .editorconfig usage.
	/// </summary>
	[TestClass]
	public class LimitConditionComplexityAnalyzerTest2 : DiagnosticVerifier
	{
		private const string CorrectCode = @"
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

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(CorrectCode, DisplayName = nameof(CorrectCode))]
		public void WhenConditionIsTooComplexDiagnosticIsTriggered(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(Common.DiagnosticIds.LimitConditionComplexity);
			VerifyCSharpDiagnostic(testCode, expected);
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
				{ key, "not a number" }
			};
			return options;
		}
	}
}
