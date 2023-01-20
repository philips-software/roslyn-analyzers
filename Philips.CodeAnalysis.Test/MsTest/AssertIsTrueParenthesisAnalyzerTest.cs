// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

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
				Id = Helper.ToDiagnosticId(DiagnosticIds.AssertIsTrueParenthesis),
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
		[DataRow("Assert.IsTrue(((1 == 2)))", "Assert.IsTrue((1 == 2))")]
		[DataRow("Assert.IsFalse(((1 == 2)))", "Assert.IsFalse((1 == 2))")]
		public void ParenthesisAreRemoved(string given, string expected)
		{
			VerifyChange(given, expected, expectedErrorColumnOffset: given.IndexOf("(") + 1);
		}
	}
}
