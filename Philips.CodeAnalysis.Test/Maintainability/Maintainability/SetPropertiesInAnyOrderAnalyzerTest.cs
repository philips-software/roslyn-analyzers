﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
        private int One { get; set; }
        private int Two { get; set; }
    }
}";

		private const string WrongInAssignment = @"
namespace PropertiesinOrderTests {
    public class Number {
        private int One { get; }
        private int Two {
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
		 DataRow(CorrectAutoProperties, DisplayName = nameof(CorrectAutoProperties))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongInAssignment, DisplayName = nameof(WrongInAssignment))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenReferencingOtherPropertiesDiagnosticIsRaised(string testCode) {
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.SetPropertiesInAnyOrder);
			VerifyDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifyDiagnostic(WrongInAssignment, filePath);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
			return new SetPropertiesInAnyOrderAnalyzer();
		}
	}
}
