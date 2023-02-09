// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class AssertIsTrueParenthesisAnalyzerTest : AssertCodeFixVerifier
	{
		public AssertIsTrueParenthesisAnalyzerTest()
		{
			DefaultMethodAttributes = @"[TestMethod]";
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AssertIsTrueParenthesis),
				Message = new Regex(".+"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
						  {
							new DiagnosticResultLocation("Test0.cs", 15 + expectedLineNumberErrorOffset, 7 + expectedColumnErrorOffset)
						}
			};
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AssertIsTrueParenthesisCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AssertIsTrueParenthesisAnalyzer();
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue((1 == 2))", "Assert.IsTrue(1 == 2)")]
		[DataRow("Assert.IsFalse((1 == 2))", "Assert.IsFalse(1 == 2)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParenthesisAreRemoved(string given, string expected)
		{
			await VerifyChange(given, expected, expectedErrorColumnOffset: given.IndexOf("(") + 1).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(((1 == 2)))", "Assert.IsTrue((1 == 2))")]
		[DataRow("Assert.IsFalse(((1 == 2)))", "Assert.IsFalse((1 == 2))")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NestedParenthesisAreRemoved(string given, string expected)
		{
			// This will fix one set of paranetheses, but will re-trip on the next layer
			await VerifyChange(given, expected, expectedErrorColumnOffset: given.IndexOf("(") + 1, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}
	}
}
