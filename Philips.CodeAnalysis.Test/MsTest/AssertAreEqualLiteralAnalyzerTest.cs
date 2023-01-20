// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class AssertAreEqualLiteralAnalyzerTest : AssertCodeFixVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AssertAreEqualLiteral),
				Location = new DiagnosticResultLocation("Test0.cs", null, null),
				Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
			};
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AssertAreEqualLiteralAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AssertAreEqualLiteralCodeFixProvider();
		}

		#endregion

		#region Public Interface

		[DataTestMethod]
		[DataRow("Assert.AreEqual(true, true)")]
		[DataRow("Assert.AreEqual(false, false)")]
		[DataRow("Assert.AreEqual(!false, true)")]
		[DataRow("Assert.AreEqual(true, !false)")]
		[DataRow("Assert.AreEqual(false, true)")]
		[DataRow("Assert.AreEqual(true, false)")]
		[DataRow("Assert.AreNotEqual(true, true)")]
		[DataRow("Assert.AreNotEqual(false, false)")]
		[DataRow("Assert.AreNotEqual(!false, true)")]
		[DataRow("Assert.AreNotEqual(true, !false)")]
		[DataRow("Assert.AreNotEqual(false, true)")]
		[DataRow("Assert.AreNotEqual(true, false)")]
		public void CheckLiteral(string given)
		{
			VerifyError(given);
		}

		[DataTestMethod]
		[DataRow("Assert.AreEqual(true, Get())", "Assert.IsTrue(Get())")]
		[DataRow("Assert.AreEqual(true, Get(), \"hi\")", "Assert.IsTrue(Get(), \"hi\")")]
		[DataRow("Assert.AreEqual(false, Get())", "Assert.IsFalse(Get())")]
		[DataRow("Assert.AreNotEqual(true, Get())", "Assert.IsFalse(Get())")]
		[DataRow("Assert.AreNotEqual(false, Get())", "Assert.IsTrue(Get())")]
		[DataRow("Assert.AreEqual(!true, Get())", "Assert.IsFalse(Get())")]
		[DataRow("Assert.AreEqual(!false, Get())", "Assert.IsTrue(Get())")]
		[DataRow("Assert.AreNotEqual(!true, Get())", "Assert.IsTrue(Get())")]
		[DataRow("Assert.AreNotEqual(!false, Get())", "Assert.IsFalse(Get())")]
		[DataRow("Assert.AreEqual(true, !Get())", "Assert.IsTrue(!Get())")]
		[DataRow("Assert.AreEqual(false, !Get())", "Assert.IsFalse(!Get())")]
		[DataRow("Assert.AreNotEqual(true, !Get())", "Assert.IsFalse(!Get())")]
		[DataRow("Assert.AreNotEqual(false, !Get())", "Assert.IsTrue(!Get())")]
		[DataRow("Assert.AreEqual(!true, !Get())", "Assert.IsFalse(!Get())")]
		[DataRow("Assert.AreEqual(!false, !Get())", "Assert.IsTrue(!Get())")]
		[DataRow("Assert.AreNotEqual(!true, !Get())", "Assert.IsTrue(!Get())")]
		[DataRow("Assert.AreNotEqual(!false, !Get())", "Assert.IsFalse(!Get())")]
		public void CheckLiteralChanged(string given, string expected)
		{
			OtherClassSyntax = @"
static bool Get() { return true; }
";

			VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Assert.AreEqual(true, (bool?)true")]
		[DataRow("Assert.AreEqual(true, Get())")]
		[DataRow("Assert.AreEqual(false, Get())")]
		[DataRow("Assert.AreNotEqual(true, Get())")]
		[DataRow("Assert.AreNotEqual(false, Get())")]
		[DataRow("Assert.AreEqual(!true, Get())")]
		[DataRow("Assert.AreEqual(!false, Get())")]
		[DataRow("Assert.AreNotEqual(!true, Get())")]
		[DataRow("Assert.AreNotEqual(!false, Get())")]
		[DataRow("Assert.AreEqual(true, !Get())")]
		[DataRow("Assert.AreEqual(false, !Get())")]
		[DataRow("Assert.AreNotEqual(true, !Get())")]
		[DataRow("Assert.AreNotEqual(false, !Get())")]
		[DataRow("Assert.AreEqual(!true, !Get())")]
		[DataRow("Assert.AreEqual(!false, !Get())")]
		[DataRow("Assert.AreNotEqual(!true, !Get())")]
		[DataRow("Assert.AreNotEqual(!false, !Get())")]
		public void CheckLiteralNoChange(string given)
		{
			OtherClassSyntax = @"
static bool? Get() { return true; }
";

			VerifyNoChange(given);
		}

		[TestMethod]
		public void AssertNullables()
		{
			// Confirm behavior of Assert.
			Assert.AreEqual(true, (bool?)true);
			Assert.AreNotEqual(true, (bool?)false);
			Assert.AreNotEqual(true, (bool?)null);
			// Assert.IsTrue((bool?)true);  Does not compile.
		}
		#endregion
	}
}
