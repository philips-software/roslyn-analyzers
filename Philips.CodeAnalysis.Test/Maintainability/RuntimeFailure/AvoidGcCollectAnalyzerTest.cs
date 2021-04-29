// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="AvoidGcCollectAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidGcCollectAnalyzerTest : DiagnosticVerifier
	{

		private const string Correct = @"
    namespace GcCollectUnitTests {
        public class Program {
            public bool Main() {
                return true;
            }
        }
    }";

		private const string Violation = @"
    using System;

    namespace GcCollectUnitTests {
        public class Program {
            public bool Main() {
                GC.Collect();
                return false;
            }
        }
    }";

		/// <summary>
		/// No diagnostics expected to show up. 
		/// </summary>
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(Correct, DisplayName = "Correct")]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostic is expected to show up. 
		/// </summary>
		[TestMethod]
		[DataRow(Violation, 7, 17, DisplayName = "Violation")]
		public void WhenGcCollectIsCalledDiagnosticIsRaised(string testCode, int line, int column)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.AvoidGcCollect);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow(Violation, "Test.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyCSharpDiagnostic(testCode, filePath);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidGcCollectAnalyzer();
		}
	}
}
