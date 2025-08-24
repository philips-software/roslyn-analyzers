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

		private const string MethodCallAssignmentViolation = @"
namespace AssignmentInConditionUnitTests {
	public class Program {
		public bool Main() {
			string result;
			if (result = GetValue()) {
				// Do something
			}
		}
		
		private string GetValue() {
			return ""test"";
		}
	}
}";

		private const string MethodCallAssignmentFixed = @"
namespace AssignmentInConditionUnitTests {
	public class Program {
		public bool Main() {
			string result;
			result = GetValue();
			if (result) {
				// Do something
			}
		}
		
		private string GetValue() {
			return ""test"";
		}
	}
}";

		private const string ComplexTernaryMethodCallViolation = @"
namespace AssignmentInConditionUnitTests {
	public class Program {
		public bool Main() {
			string result;
			int value = (result = GetValue()) != null ? 10 : 20;
		}
		
		private string GetValue() {
			return ""test"";
		}
	}
}";

		private const string ComplexTernaryMethodCallFixed = @"
namespace AssignmentInConditionUnitTests {
	public class Program {
		public bool Main() {
			string result;
			result = GetValue();
			int value = (result) != null ? 10 : 20;
		}
		
		private string GetValue() {
			return ""test"";
		}
	}
}";

		[DataTestMethod]
		[DataRow(SimpleAssignmentViolation, SimpleAssignmentFixed, DisplayName = "SimpleAssignment")]
		[DataRow(TernaryViolation, TernaryFixed, DisplayName = "Ternary")]
		[DataRow(MethodCallAssignmentViolation, MethodCallAssignmentFixed, DisplayName = "MethodCallAssignment")]
		[DataRow(ComplexTernaryMethodCallViolation, ComplexTernaryMethodCallFixed, DisplayName = "ComplexTernaryMethodCall")]
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