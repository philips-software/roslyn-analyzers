// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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
		[DataTestMethod]
		[DataRow(Correct, DisplayName = nameof(Correct)),
			DataRow(CorrectSingle, DisplayName = nameof(CorrectSingle))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Wrong, DisplayName = nameof(Wrong))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenConditionIsTooComplexDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifyDiagnostic(testCode, DiagnosticId.LimitConditionComplexity).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(Wrong, "Dummy.Designer", DisplayName = "OutOfScopeSourceFile")]
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
			return new LimitConditionComplexityAnalyzer();
		}

		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			var key =
				$@"dotnet_code_quality.{Helper.ToDiagnosticId(DiagnosticId.LimitConditionComplexity)}.max_operators";
			return base.GetAdditionalAnalyzerConfigOptions().Add(key, ConfiguredMaxOperators.ToString());
		}
	}
}
