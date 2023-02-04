// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidAssertConditionalAccessAnalyzerFailTest(string test)
		{
			VerifyError(test, Helper.ToDiagnosticId(DiagnosticId.AvoidAssertConditionalAccess));
		}

		
		[DataTestMethod]
		[DataRow("string name1=\"xyz\"; string name2=\"abc\"; Assert.AreEqual((name1?.ToString()), (name2?.ToString()))")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidAssertConditionalAccessAnalyzerFailTestMultipleErrors(string test)
		{
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.AvoidAssertConditionalAccess),
					Severity = DiagnosticSeverity.Error,
					Location = new DiagnosticResultLocation("Test0.cs", null, null),
				},
				new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.AvoidAssertConditionalAccess),
					Severity = DiagnosticSeverity.Error,
					Location = new DiagnosticResultLocation("Test0.cs", null, null),
				}
			};
			AssertCodeHelper helper = new();
			var code = helper.GetText(test, string.Empty, string.Empty);
			VerifyDiagnostic(code, expected);
		}
		

		[DataTestMethod]
		[DataRow("string name=\"xyz\"; Assert.AreEqual(name.ToString(), \"xyz\")")]
		[DataRow("string name=\"xyz\"; Assert.AreEqual(\"xyz\",name.ToString())")]
		[DataRow("string name1=\"xyz\"; string name2=\"abc\"; Assert.AreEqual(name1.ToString(), name2.ToString())")]
		[DataRow("string name1=\"xyz\"; string name2=\"abc\"; Assert.AreEqual(name1.ToString(), name2.ToString(), $\"error{name1?.ToString()}\")")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidAssertConditionalAccessAnalyzerSuccessTest(string test)
		{
			VerifySuccessfulCompilation(test);
		}
		#endregion
	}
}
