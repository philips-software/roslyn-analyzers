// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

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
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectField, DisplayName = nameof(CorrectField)),
		 DataRow(CorrectLocal, DisplayName = nameof(CorrectLocal))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(WrongField, FixedField, DisplayName = nameof(WrongField)), 
		 DataRow(WrongFieldFullNamespace, null, DisplayName = nameof(WrongFieldFullNamespace)),
		 DataRow(WrongLocal, FixedLocal, DisplayName = nameof(WrongLocal))]
		public void WhenMismatchOfPlusMinusDiagnosticIsRaised(string testCode, string fixedCode) {
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.AvoidArrayList);
			VerifyCSharpDiagnostic(testCode, expected);
			if (fixedCode != null)
			{
				VerifyCSharpFix(testCode, fixedCode, allowNewCompilerDiagnostics:true);
			}
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifyCSharpDiagnostic(WrongLocal, filePath);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new AvoidArrayListAnalyzer();
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new AvoidArrayListCodeFixProvider();
		}
	}
}
