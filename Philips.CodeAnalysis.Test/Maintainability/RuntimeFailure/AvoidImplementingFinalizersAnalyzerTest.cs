// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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
        protected void Dispose(bool isDisposing) {
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
        protected void Dispose(bool isDisposing) {
        }
    }
}";

		private const string WrongFieldAssignment = @"
namespace FinalizerTest {
    class Program {
        int i;
        ~Program() {
            i = 0;
            Dispose(false);
        }
        protected void Dispose(bool isDisposing) {
        }
    }
}";

		private const string WrongOtherMethod = @"
namespace FinalizerTest {
    class Program {
        ~Program() {
            Cleanup();
        }
        protected void Cleanmup() {
        }
    }
}";

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenFinalizerHasOnlyDisposeNoDiagnosticIsTriggeredAsync()
		{
			await VerifySuccessfulCompilation(CorrectWithDispose).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(WrongNoDispose, DisplayName = nameof(WrongNoDispose)),
		 DataRow(WrongReferenceCall, DisplayName = nameof(WrongReferenceCall)),
		 DataRow(WrongFieldAssignment, DisplayName = nameof(WrongFieldAssignment)),
		 DataRow(WrongOtherMethod, DisplayName = nameof(WrongOtherMethod))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenFinalizerMissesDisposeNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidImplementingFinalizersAnalyzer();
		}
	}
}
