// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	/// Test class for <see cref="SetPropertiesInAnyOrderAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class SetPropertiesInAnyOrderAnalyzerTest : DiagnosticVerifier
	{
		private const string CorrectAutoProperties = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; set; }
        public int Two { get; set; }
    }
}";

		private const string CorrectInitializer = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; set; } = 1;
        public int Two { get; set; }
    }
}";

		private const string CorrectNoSetter = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; };
        public int Two { get; set; }
    }
}";

		private const string CorrectPrivateSetter = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; private set; };
        public int Two { get; set; }
    }
}";

		private const string WrongInAssignment = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; set; }
        public int Two {
            set {
                One = value - 1;
            }
        }
    }
}";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectAutoProperties, DisplayName = nameof(CorrectAutoProperties)),
		 DataRow(CorrectInitializer, DisplayName = nameof(CorrectInitializer)),
		 DataRow(CorrectNoSetter, DisplayName = nameof(CorrectNoSetter)),
		 DataRow(CorrectPrivateSetter, DisplayName = nameof(CorrectPrivateSetter))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongInAssignment, DisplayName = nameof(WrongInAssignment))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenReferencingOtherPropertiesDiagnosticIsRaisedAsync(string testCode)
		{
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
			await VerifySuccessfulCompilation(WrongInAssignment, filePath).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new SetPropertiesInAnyOrderAnalyzer();
		}
	}
}
