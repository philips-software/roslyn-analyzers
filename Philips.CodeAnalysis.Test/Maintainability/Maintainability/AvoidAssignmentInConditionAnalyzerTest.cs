// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="AvoidAssignmentInConditionAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidAssignmentInConditionAnalyzerTest : DiagnosticVerifier
	{
		private const string Correct = @"
    namespace AssignmentInConditionUnitTests {
        public class Program {
            public bool Main() {
                int i = 1;
                if (i == 2) {
                    // Do nothing
                }
            }
        }
    }";

		private const string CorrectTernary = @"
    namespace AssignmentInConditionUnitTests {
        public class Program {
            public bool Main() {
                int i = 1;
                (i == 2) ? i = 3 : i = 4;
            }
        }
    }";

		private const string CorrectPropertyAssignment = @"
    namespace AssignmentInConditionUnitTests {
        public class Program {
            public bool Main() {
                theProperty = 1;
                if (theProperty == 2) {
                    // Do nothing
                }
            }

            private string theProperty { get; private set; }
        }
    }";

		private const string CorrectUsing = @"
    namespace AssignmentInConditionUnitTests {
        public class Program {
            public bool Main() {
                using (cts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, token)) {
                    // Do nothing
                }
            }

            private string theProperty { get; private set; }
        }
    }";

		private const string CorrectInitializer = @"
    namespace AssignmentInConditionUnitTests {
        public class Program {
            public bool Main() {
                var context = new ResourceContext { BodyStream = readStream };
            }

            private string theProperty { get; private set; }
        }
    }";

		private const string CorrectNullCoalescing = @"
    namespace AssignmentInConditionUnitTests {
        public class Program {
            public bool Main() {
                string str;
                str ?? (str = 'Hello World!');
            }

            private string theProperty { get; private set; }
        }
    }";


		private const string Violation = @"
    namespace AssignmentInConditionUnitTests {
        public class Program {
            public bool Main() {
                int i = 1;
                if (i = 2) {
                    // Do nothing
                }
            }
        }
    }";

		private const string ViolationTernary = @"
    namespace AssignmentInConditionUnitTests {
        public class Program {
            public bool Main() {
                int i = 1;
                (i = 2) ? i = 3 : i = 4;
            }
        }
    }";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(Correct, DisplayName = "Correct"),
		 DataRow(CorrectTernary, DisplayName = "CorrectTernary"),
		 DataRow(CorrectUsing, DisplayName = "CorrectUsing"),
		 DataRow(CorrectInitializer, DisplayName = "CorrectInitializer"),
		 DataRow(CorrectPropertyAssignment, DisplayName = "CorrectPropertyAssignment"),
		 DataRow(CorrectNullCoalescing, DisplayName = "CorrectNullCoalescing")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow(Violation, DisplayName = "Violation"),
		 DataRow(ViolationTernary, DisplayName = "ViolationTernary")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenDoingAssignmentInsideConditionDiagnosticIsRaisedAsync(string testCode) {
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggeredAsync(string filePath)
		{
			await VerifySuccessfulCompilation(Violation, filePath).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAssignmentInConditionAnalyzer();
		}
	}
}
