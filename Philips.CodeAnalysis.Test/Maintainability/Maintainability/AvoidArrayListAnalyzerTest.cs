// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="AvoidArrayListAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidArrayListAnalyzerTest : CodeFixVerifier
	{
		private const string CorrectField = @"
using System.Collections.Generic;
namespace AvoidArrayListTests {
    public class Number {
        private List<int> nn;
    }
}";

		private const string CorrectLocal = @"
using System.Collections.Generic;
namespace AvoidArrayListTests {
    public class Number {
        public Number() {
            List<int> nn;
        }
    }
}";

		private const string WrongField = @"
using System.Collections;
namespace AvoidArrayListTests {
    public class Number {
		private ArrayList nn;
    }
}";
		private const string FixedField = @"
using System.Collections;
namespace AvoidArrayListTests {
    public class Number {
		private List<int> nn;
    }
}";

		private const string WrongFieldFullNamespace = @"
namespace AvoidArrayListTests {
    public class Number {
		private System.Collections.ArrayList nn;
    }
}";

		private const string WrongLocal = @"
using System.Collections;
namespace AvoidArrayListTests {
    public class Number {
        public Number() {
            ArrayList nn = new ArrayList();
        }
    }
}";
		private const string FixedLocal = @"
using System.Collections;
namespace AvoidArrayListTests {
    public class Number {
        public Number() {
            List<int> nn = new List<int>();
        }
    }
}";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectField, DisplayName = nameof(CorrectField)),
		 DataRow(CorrectLocal, DisplayName = nameof(CorrectLocal))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongField, FixedField, DisplayName = nameof(WrongField)), 
		 DataRow(WrongFieldFullNamespace, null, DisplayName = nameof(WrongFieldFullNamespace)),
		 DataRow(WrongLocal, FixedLocal, DisplayName = nameof(WrongLocal))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenMismatchOfPlusMinusDiagnosticIsRaised(string testCode, string fixedCode) {
			var expected = DiagnosticResultHelper.Create(DiagnosticId.AvoidArrayList);
			VerifyDiagnostic(testCode, expected);
			if (fixedCode != null)
			{
				VerifyFix(testCode, fixedCode, allowNewCompilerDiagnostics:true);
			}
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifyDiagnostic(WrongLocal, filePath);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
			return new AvoidArrayListAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidArrayListCodeFixProvider();
		}
	}
}
