// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{

	[TestClass]
	public class AvoidAssertConditionalAccessAnalyzerTest : AssertDiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAssertConditionalAccessAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataTestMethod]
		[DataRow("string name=\"xyz\"; Assert.AreEqual(name?.ToString(), \"xyz\")")]
		[DataRow("string name=\"xyz\"; Assert.AreEqual(\"xyz\",name?.ToString())")]
		[DataRow("string name1=\"xyz\"; string name2=\"abc\"; Assert.AreEqual((name1?.ToString()), name2.ToString())")]
		[DataRow("string name1=\"xyz\"; string name2=\"abc\"; Assert.AreEqual(name1.ToString(), (name2?.ToString()))")]
		public void AvoidAssertConditionalAccessAnalyzerFailTest(string test)
		{
			VerifyError(test, Helper.ToDiagnosticId(DiagnosticIds.AvoidAssertConditionalAccess));
		}

		[DataTestMethod]
		[DataRow("string name1=\"xyz\"; string name2=\"abc\"; Assert.AreEqual((name1?.ToString()), (name2?.ToString()))")]
		public void AvoidAssertConditionalAccessAnalyzerFailTestMultipleErrors(string test)
		{
			VerifyError(test, Helper.ToDiagnosticId(DiagnosticIds.AvoidAssertConditionalAccess), Helper.ToDiagnosticId(DiagnosticIds.AvoidAssertConditionalAccess));
		}

		[DataTestMethod]
		[DataRow("string name=\"xyz\"; Assert.AreEqual(name.ToString(), \"xyz\")")]
		[DataRow("string name=\"xyz\"; Assert.AreEqual(\"xyz\",name.ToString())")]
		[DataRow("string name1=\"xyz\"; string name2=\"abc\"; Assert.AreEqual(name1.ToString(), name2.ToString())")]
		[DataRow("string name1=\"xyz\"; string name2=\"abc\"; Assert.AreEqual(name1.ToString(), name2.ToString(), $\"error{name1?.ToString()}\")")]
		public void AvoidAssertConditionalAccessAnalyzerSuccessTest(string test)
		{
			VerifyCSharpDiagnostic(test, Array.Empty<DiagnosticResult>());
		}
		#endregion
	}
}
