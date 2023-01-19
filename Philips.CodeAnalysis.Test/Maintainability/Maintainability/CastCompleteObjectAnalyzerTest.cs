// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

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
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectSingleField, DisplayName = nameof(CorrectSingleField)),
		 DataRow(CorrectOtherType, DisplayName = nameof(CorrectOtherType))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(WrongMulipleFields, DisplayName = nameof(WrongMulipleFields))]
		public void WhenMismatchOfPlusMinusDiagnosticIsRaised(string testCode) {
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.CastCompleteObject);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifyCSharpDiagnostic(WrongMulipleFields, filePath);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new CastCompleteObjectAnalyzer();
		}
	}
}
