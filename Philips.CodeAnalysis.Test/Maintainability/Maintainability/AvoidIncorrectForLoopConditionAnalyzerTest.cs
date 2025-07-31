// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="AvoidIncorrectForLoopConditionAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidIncorrectForLoopConditionAnalyzerTest : DiagnosticVerifier
	{
		private const string CorrectBackwardsLoop = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            for (var index = list.Count - 1; index >= 0; index--) {
                // Process element at index
            }
        }
    }
}";

		private const string CorrectForwardLoop = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            for (var index = 0; index < list.Count; index++) {
                // Process element at index
            }
        }
    }
}";

		private const string CorrectComplexCondition = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            for (var index = list.Count - 1; index >= 0 && list[index] > 0; index--) {
                // Process element at index
            }
        }
    }
}";

		private const string CorrectDifferentVariable = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            int otherVar = 5;
            for (var index = list.Count - 1; otherVar > 0; index--) {
                otherVar--;
            }
        }
    }
}";

		private const string ViolationPostDecrement = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            for (var index = list.Count - 1; index > 0; index--) {
                // This will miss element at index 0
            }
        }
    }
}";

		private const string ViolationPreDecrement = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            for (var index = list.Count - 1; index > 0; --index) {
                // This will miss element at index 0
            }
        }
    }
}";

		private const string ViolationSubtractAssignment = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            for (var index = list.Count - 1; index > 0; index -= 1) {
                // This will miss element at index 0
            }
        }
    }
}";

		private const string ViolationSimpleAssignment = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            for (var index = list.Count - 1; index > 0; index = index - 1) {
                // This will miss element at index 0
            }
        }
    }
}";

		private const string ViolationReversedComparison = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            for (var index = list.Count - 1; 0 < index; index--) {
                // This will miss element at index 0
            }
        }
    }
}";

		[DataTestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectBackwardsLoop, DisplayName = nameof(CorrectBackwardsLoop)),
		 DataRow(CorrectForwardLoop, DisplayName = nameof(CorrectForwardLoop)),
		 DataRow(CorrectComplexCondition, DisplayName = nameof(CorrectComplexCondition)),
		 DataRow(CorrectDifferentVariable, DisplayName = nameof(CorrectDifferentVariable))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(ViolationPostDecrement, DisplayName = "ViolationPostDecrement"),
		 DataRow(ViolationPreDecrement, DisplayName = "ViolationPreDecrement"),
		 DataRow(ViolationSubtractAssignment, DisplayName = "ViolationSubtractAssignment"),
		 DataRow(ViolationSimpleAssignment, DisplayName = "ViolationSimpleAssignment"),
		 DataRow(ViolationReversedComparison, DisplayName = "ViolationReversedComparison")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenBackwardsLoopHasIncorrectConditionDiagnosticIsRaised(string testCode)
		{
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("File.g")]
		[DataRow("GlobalSuppressions")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			await VerifySuccessfulCompilation(ViolationPostDecrement, filePath).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidIncorrectForLoopConditionAnalyzer();
		}
	}
}
