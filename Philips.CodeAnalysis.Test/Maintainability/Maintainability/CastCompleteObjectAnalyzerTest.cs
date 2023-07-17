// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="CastCompleteObjectAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class CastCompleteObjectAnalyzerTest : DiagnosticVerifier
	{
		private const string CorrectSingleField = @"
namespace CastCompleteTests {
    public class Number {
        private int n;
        public static explicit operator int(Number num) { return num.n; }
    }
}";

		private const string CorrectOtherType = @"
namespace CastCompleteTests {
    public class Number {
        private int n;
        public static explicit operator double(Number num) { return (double)num.n; }
    }
}";

		private const string WrongMultipleFields = @"
namespace CastCompleteTests {
    public class Number {
        private int n;
        private string str;
        public static explicit operator int(Number num) { return num.n; }
    }
}";

		[DataTestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectSingleField, DisplayName = nameof(CorrectSingleField)),
		 DataRow(CorrectOtherType, DisplayName = nameof(CorrectOtherType))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(WrongMultipleFields, DisplayName = nameof(WrongMultipleFields))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsInvalidDiagnosticIsTriggered(string testCode)
		{
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("File.g")]
		[DataRow("GlobalSuppressions")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			await VerifySuccessfulCompilation(WrongMultipleFields, filePath).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new CastCompleteObjectAnalyzer();
		}
	}
}
