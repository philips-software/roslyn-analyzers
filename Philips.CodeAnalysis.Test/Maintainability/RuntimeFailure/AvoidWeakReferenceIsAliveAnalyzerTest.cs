// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="AvoidWeakReferenceIsAliveAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidWeakReferenceIsAliveAnalyzerTest : DiagnosticVerifier
	{

		private const string OtherType = @"
    namespace WeakReferenceIsAliveUnitTests {
        public class WeakRef {
            public bool IsAlive { get; }
        }

        public class Program {
            public bool Main() {
                var weak = new WeakRef();
                return weak.IsAlive;
            }
        }
    }";

		private const string Violation = @"
    using System;

    namespace WeakReferenceIsAliveUnitTests {
        public class Program {
            public bool Main() {
                var weak = new WeakReference();
                return weak.IsAlive;
            }
        }
    }";

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow("", DisplayName = "Correct"),
		 DataRow(OtherType, DisplayName = "OtherType")]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostic is expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(Violation, DisplayName = "Violation")]
		public void WhenWeakReferenceIsAliveIsCalledDiagnosticIsRaised(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.AvoidWeakReferenceIsAlive);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow(Violation, "Test.Designer", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyCSharpDiagnostic(testCode, filePath);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidWeakReferenceIsAliveAnalyzer();
		}
	}
}
