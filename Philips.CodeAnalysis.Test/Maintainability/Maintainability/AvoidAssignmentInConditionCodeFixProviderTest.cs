// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="AvoidAssignmentInConditionCodeFixProvider"/>.
	/// </summary>
	[TestClass]
	public class AvoidAssignmentInConditionCodeFixProviderTest : CodeFixVerifier
	{
		private const string SimpleAssignmentViolation = @"
namespace AssignmentInConditionUnitTests {
	public class Program {
		public bool Main() {
			bool flag = false;
			if (flag = true) {
				// Do nothing
			}
		}
	}
}";

		private const string SimpleAssignmentFixed = @"
namespace AssignmentInConditionUnitTests {
	public class Program {
		public bool Main() {
			bool flag = false;
			flag = true;
			if (flag) {
				// Do nothing
			}
		}
	}
}";

		private const string TernaryViolation = @"
namespace AssignmentInConditionUnitTests {
	public class Program {
		public bool Main() {
			bool flag = false;
			int result = (flag = true) ? 10 : 20;
		}
	}
}";

		private const string TernaryFixed = @"
namespace AssignmentInConditionUnitTests {
	public class Program {
		public bool Main() {
			bool flag = false;
			flag = true;
			int result = (flag) ? 10 : 20;
		}
	}
}";

		[DataTestMethod]
		[DataRow(SimpleAssignmentViolation, SimpleAssignmentFixed, DisplayName = "SimpleAssignment")]
		[DataRow(TernaryViolation, TernaryFixed, DisplayName = "Ternary")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenAssignmentInConditionCodeFixIsApplied(string testCode, string fixedCode)
		{
			await VerifyFix(testCode, fixedCode).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAssignmentInConditionAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidAssignmentInConditionCodeFixProvider();
		}
	}
}