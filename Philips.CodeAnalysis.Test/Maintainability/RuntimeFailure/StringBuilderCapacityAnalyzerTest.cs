// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="StringBuilderCapacityAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class StringBuilderCapacityAnalyzerTest : DiagnosticVerifier
	{

		private const string Correct = @"
    using System.Text;

    namespace StringBuilderCapacityUnitTests {
        public class Program {
            public string Main() {
                int capacity = 42;
                var builder = new StringBuilder(capacity);
                return builder.ToString();
            }
        }
    }";

		private const string Violation = @"
    using System.Text;

    namespace StringBuilderCapacityUnitTests {
        public class Program {
            public string Main() {
                var builder = new StringBuilder();
                return builder.ToString();
            }
        }
    }";

		private const string OtherClass = @"
    using System;

    namespace StringBuilderCapacityUnitTests {
        public class StingBuilder {
        }
        public class Program {
            public string Main() {
                int capacity = 42;
                var builder = new StingBuilder();
                return builder.ToString();
            }
        }
    }";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(Correct, DisplayName = nameof(Correct)),
		 DataRow(OtherClass, DisplayName = nameof(OtherClass))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(Violation, "builder", 7, 31, DisplayName = nameof(Violation))]
		public void WhenStringBuilderIsCreatedWithoutCapacityDiagnosticIsRaised(
			string testCode,
			string identifier,
			int line,
			int column
		)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.StringBuilderCapacity);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow(Violation, "Test.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyCSharpDiagnostic(testCode, filePath);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new StringBuilderCapacityAnalyzer();
		}
	}
}
