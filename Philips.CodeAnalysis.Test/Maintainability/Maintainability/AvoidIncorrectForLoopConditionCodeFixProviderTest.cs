// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="AvoidIncorrectForLoopConditionCodeFixProvider"/>.
	/// </summary>
	[TestClass]
	public class AvoidIncorrectForLoopConditionCodeFixProviderTest : CodeFixVerifier
	{
		private const string TestCode = @"
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

		private const string FixedCode = @"
using System.Collections.Generic;
namespace ForLoopTests {
    public class Program {
        public void Method() {
            var list = new List<int> { 1, 2, 3 };
            for (var index = list.Count - 1; index >= 0; index--) {
                // This will miss element at index 0
            }
        }
    }
}";

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixChangesGreaterThanToGreaterThanOrEqual()
		{
			await VerifyDiagnosticAndFix(TestCode, FixedCode).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidIncorrectForLoopConditionCodeFixProvider();
		}

		protected override Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidIncorrectForLoopConditionAnalyzer();
		}
	}
}