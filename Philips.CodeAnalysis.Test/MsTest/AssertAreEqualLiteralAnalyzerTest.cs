// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
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
	public class AssertAreEqualLiteralAnalyzerTest : AssertCodeFixVerifier
	{
		#region Non-Public Properties/Methods

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult()
			{
				Id = DiagnosticId.AssertAreEqualLiteral.ToId(),
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

		[TestMethod]
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckLiteralAsync(string given)
		{
			await VerifyError(given).ConfigureAwait(false);
		}

		[TestMethod]
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckLiteralChanged(string given, string expected)
		{
			OtherClassSyntax = @"
static bool Get() { return true; }
";

			await VerifyChange(given, expected).ConfigureAwait(false);
		}

		[TestMethod]
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckLiteralNoChange(string given)
		{
			OtherClassSyntax = @"
static bool? Get() { return true; }
";

			await VerifyNoChange(given).ConfigureAwait(false);
		}

		#endregion
	}
}
