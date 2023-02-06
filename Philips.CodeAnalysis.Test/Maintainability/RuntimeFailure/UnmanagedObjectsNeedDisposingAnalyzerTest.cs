// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="UnmanagedObjectsNeedDisposingAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class UnmanagedObjectsNeedDisposingAnalyzerTest : DiagnosticVerifier
	{
		private static string CreateClass(string fieldType, bool isDisposable)
		{
			var faces = string.Empty;
			if (isDisposable)
			{
				faces = ": IDisposable";
			}
			string baseline = @"
namespace MyNamespace
{{
  class FooClass{1}
  {{
    private {0} f;
  }}
}}
";

			return string.Format(baseline, fieldType, faces);
		}

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow("IntPtr", true, DisplayName = "CorrectIntPtrDisposable"),
		 DataRow("HANDLE", true, DisplayName = "CorrectHandleDisposable"),
		 DataRow("int", false, DisplayName = "CorrectIntNotDisposable")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string fieldType, bool isDisposable)
		{
			var source = CreateClass(fieldType, isDisposable);
			VerifySuccessfulCompilation(source);
		}

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoDiagnosticIsTriggeredForStructs()
		{
			string source = @"
namespace MyNamespace
{{
  struct FooStruct
  {{
    private IntPtr f;
  }}
}}
";
			VerifySuccessfulCompilation(source);
		}

		/// <summary>
		/// Diagnostic is expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow("IntPtr", DisplayName = "WrongIntPtrNotDisposable"),
		 DataRow("Handle", DisplayName = "WrongHandleNotDisposable")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenNotDisposableHasUnmanagedFieldsDiagnosticIsRaised(string fieldType)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticId.UnmanagedObjectsNeedDisposing);
			var source = CreateClass(fieldType, false);
			VerifyDiagnostic(source, expected);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
			return new UnmanagedObjectsNeedDisposingAnalyzer();
		}
	}
}
