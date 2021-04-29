// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="AvoidMethodImplSynchronizedAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidMethodImplSynchronizedUnitTests : DiagnosticVerifier
	{

		private const string NoAttribute = @"
    using System;
    using System.Runtime.CompilerServices;

    namespace MethodImplSynchronizedUnitTests {
        public class Program {
            public bool Main() {
                return true;
            }
        }
    }";

		private const string OtherOptionsMethod = @"
    using System;
    using System.Runtime.CompilerServices;

    namespace MethodImplSynchronizedUnitTests {
        public class Program {
            [MethodImpl(MethodImplOptions.NoInlining]
            public bool Main() {
                return true;
            }
        }
    }";

		private const string OtherOptionsClass = @"
    using System;
    using System.Runtime.CompilerServices;

    namespace MethodImplSynchronizedUnitTests {
        [MethodImpl(MethodImplOptions.NoInlining]
        public class Program {
            public bool Main() {
                return true;
            }
        }
    }";

		private const string ViolationOnMethod = @"
    using System;
    using System.Runtime.CompilerServices;

    namespace MethodImplSynchronizedUnitTests {
        public class Program {
            [MethodImpl(MethodImplOptions.Synchronized]
            public bool Main() {
                return false;
            }
        }
    }";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(NoAttribute, DisplayName = "NoAttribute"),
		 DataRow(OtherOptionsClass, DisplayName = "OtherOptionsClass"),
		 DataRow(OtherOptionsMethod, DisplayName = "OtherOptionsMethod")]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(ViolationOnMethod, DisplayName = "ViolationOnMethod")]
		public void WhenMethodHasAttributeDiagnosticIsRaised(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.AvoidMethodImplSynchronized);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow(ViolationOnMethod, "Test.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyCSharpDiagnostic(testCode, filePath);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidMethodImplSynchronizedAnalyzer();
		}
	}
}
