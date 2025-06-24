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
	public class AssertIsTrueAnalyzerTest : AssertCodeFixVerifier
	{
		public AssertIsTrueAnalyzerTest()
		{
			DefaultMethodAttributes = @"[TestMethod]";
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true == false)", "Assert.AreEqual(true, false)")]
		[DataRow("Assert.IsTrue(true != false)", "Assert.AreNotEqual(true, false)")]
		[DataRow("Assert.IsFalse(true == false)", "Assert.AreNotEqual(true, false)")]
		[DataRow("Assert.IsFalse(true != false)", "Assert.AreEqual(true, false)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ReplacesEqualsEquals(string given, string expected)
		{
			await VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Object o = null;\n      Assert.IsTrue(o == null)", "Object o = null;\n      Assert.IsNull(o)")]
		[DataRow("Object o = null;\n      Assert.IsTrue(o != null)", "Object o = null;\n      Assert.IsNotNull(o)")]
		[DataRow("Object o = null;\n      Assert.IsFalse(o == null)", "Object o = null;\n      Assert.IsNotNull(o)")]
		[DataRow("Object o = null;\n      Assert.IsFalse(o != null)", "Object o = null;\n      Assert.IsNull(o)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IsNullIsNotNull(string given, string expected)
		{
			await VerifyChange(given, expected, expectedErrorLineOffset: 1).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true == false, \"blah\")", "Assert.AreEqual(true, false, \"blah\")")]
		[DataRow("Assert.IsTrue(true != false, \"blah\")", "Assert.AreNotEqual(true, false, \"blah\")")]
		[DataRow("Assert.IsFalse(true == false, \"blah\")", "Assert.AreNotEqual(true, false, \"blah\")")]
		[DataRow("Assert.IsFalse(true != false, \"blah\")", "Assert.AreEqual(true, false, \"blah\")")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreserveMessage(string given, string expected)
		{
			await VerifyChange(given, expected).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true.Equals(false))", "Assert.AreEqual(true, false)")]
		//[DataRow("Assert.IsTrue(!true.Equals(false))", "Assert.AreNotEqual(true, false)")]
		[DataRow("Assert.IsFalse(true.Equals(false))", "Assert.AreNotEqual(true, false)")]
		[TestCategory(TestDefinitions.UnitTests)]
		//[DataRow("Assert.IsFalse(!true.Equals(false))", "Assert.AreEqual(true, false)")]
		//[DataRow("Assert.IsFalse(!true.Equals(false))", "Assert.AreEqual(true, false)")]
		public async Task ReplacesEquals(string given, string expected)
		{
			await VerifyChange(given, expected).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true && true)", "Assert.IsTrue(true);\n      Assert.IsTrue(true)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CanBreakDownCompoundStatements(string given, string expected)
		{
			await VerifyChange(given, expected).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true && true, \"blah\")", "Assert.IsTrue(true, \"blah\");\n      Assert.IsTrue(true, \"blah\")")]
		[DataRow("Assert.IsTrue(true && true, \"blah{0}\", 1)", "Assert.IsTrue(true, \"blah{0}\", 1);\n      Assert.IsTrue(true, \"blah{0}\", 1)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreserveCompoundMessages(string given, string expected)
		{
			await VerifyChange(given, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task StaticFunctionCallDoesntThrow()
		{
			var givenText = @"
class Foo 
{
  private static bool Test()
  {
    return true;
  }

  [TestMethod]
  public void Foo()
  {
    Assert.IsTrue(Test());
  }
}


";

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);

			await VerifyFix(givenText, givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CommentsAreNotRemoved()
		{
			var given = @"
int i = 50;
      //test comment
      Assert.IsTrue(true && true)";

			var expected = @"
int i = 50;
      //test comment
      Assert.IsTrue(true);
      Assert.IsTrue(true)";

			await VerifyChange(given, expected, 3).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TrailingWhitespacePreserved()
		{
			var given = @"
int i = 50;
      //test comment
      Assert.IsTrue(true && true);

      i.ToString();
      int k = 4";

			var expected = @"
int i = 50;
      //test comment
      Assert.IsTrue(true);
      Assert.IsTrue(true);

      i.ToString();
      int k = 4";

			await VerifyChange(given, expected, 3);
		}

		[DataTestMethod]
		[DataRow("Assert.IsFalse(1 != 2 && 2 == 3)")]
		[DataRow("Assert.IsTrue(1 != 2 || 2 == 3)")]
		[DataRow("Assert.IsFalse(1 != 2 || 2 == 3)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CanBreakDownFalseOrOrCompoundStatements(string given)
		{
			await VerifyNoChange(given).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("bool value = false; Assert.IsFalse(value)")]
		[DataRow("Assert.IsFalse(\"foo\".Contains(\"f\"))")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontChangeValues(string given)
		{
			await VerifyNoChange(given).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true)")]
		[DataRow("Assert.IsTrue(false)")]
		[DataRow("Assert.IsTrue(!false)")]
		[DataRow("Assert.IsFalse(true)")]
		[DataRow("Assert.IsFalse(false)")]
		[DataRow("Assert.IsFalse(!false)")]
		[DataRow("Assert.AreEqual(true, true)")]
		[DataRow("Assert.AreEqual(true, false)")]
		[DataRow("Assert.AreEqual(false, true)")]
		[DataRow("Assert.AreEqual(false, false)")]
		[DataRow("Assert.AreEqual(false, !false)")]
		[DataRow("Assert.AreEqual(!false, !false)")]
		[DataRow("Assert.AreEqual(!false, false)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckLiteral(string given)
		{
			await VerifyNoChange(given).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckNoCrash()
		{
			await VerifyNoChange("Assert.IsTrue(").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckForNoSemanticModel()
		{
			const string template = @"
Assert.IsTrue(foo.Test());
";

			await VerifyNoChange(template).ConfigureAwait(false);
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult
			{
				Id = DiagnosticId.AssertIsEqual.ToId(),
				Message = new Regex("Do not call IsTrue/IsFalse"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
						  {
							new DiagnosticResultLocation("Test0.cs", 15 + expectedLineNumberErrorOffset, 7 + expectedColumnErrorOffset)
						}
			};
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AssertIsTrueCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AssertIsTrueAnalyzer();
		}
	}
}
