// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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

        private const string WrongMulipleFields = @"
namespace CastCompleteTests {
    public class Number {
        private int n;
        private string str;
        public static explicit operator int(Number num) { return num.n; }
    }
}";
		
		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectSingleField, DisplayName = nameof(CorrectSingleField)),
		 DataRow(CorrectOtherType, DisplayName = nameof(CorrectOtherType))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongMulipleFields, DisplayName = nameof(WrongMulipleFields))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenMismatchOfPlusMinusDiagnosticIsRaised(string testCode)
		{
			VerifyDiagnostic(testCode);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifySuccessfulCompilation(WrongMulipleFields, filePath);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new CastCompleteObjectAnalyzer();
		}
	}
}
