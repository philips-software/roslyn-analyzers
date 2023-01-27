// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="AvoidImplementingFinalizersAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidImplementingFinalizersAnalyzerTest : DiagnosticVerifier
	{
		private const string CorrectWithDispose = @"
namespace FinalizerTest {
    class Program {
        ~Program() {
            Dispose(false);
        }
        protected Dispose(bool isDisposing) {
        }
    }
}";

		private const string WrongNoDispose = @"
namespace FinalizerTest {
    class Program {
        ~Program() {
        }
    }
}";

		private const string WrongReferenceCall = @"
namespace FinalizerTest {
    class Foo {
        static void Mo() {}
    }
    class Program {
        ~Program() {
            Foo.Mo();
            Dispose(false);
        }
        protected Dispose(bool isDisposing) {
        }
    }
}";

		[TestMethod]
		public void WhenFinalizerHasOnlyDisposeNoDiagnosticIsTriggered()
		{
			VerifySuccessfulCompilation(CorrectWithDispose);
		}

		[DataTestMethod]
		[DataRow(WrongNoDispose, DisplayName = nameof(WrongNoDispose)),
		 DataRow(WrongReferenceCall, DisplayName = nameof(WrongReferenceCall))]
		public void WhenFinalizerMissesDisposeNoDiagnosticIsTriggered(string testCode)
		{
			VerifyDiagnostic(testCode, DiagnosticResultHelper.Create(DiagnosticIds.AvoidImplementingFinalizers));
		}
		
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
			return new AvoidImplementingFinalizersAnalyzer();
		}
	}
}
