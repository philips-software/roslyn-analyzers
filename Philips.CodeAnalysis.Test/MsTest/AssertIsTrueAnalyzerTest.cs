// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
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
		public void ReplacesEqualsEquals(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Object o = null;\n      Assert.IsTrue(o == null)", "Object o = null;\n      Assert.IsNull(o)")]
		[DataRow("Object o = null;\n      Assert.IsTrue(o != null)", "Object o = null;\n      Assert.IsNotNull(o)")]
		[DataRow("Object o = null;\n      Assert.IsFalse(o == null)", "Object o = null;\n      Assert.IsNotNull(o)")]
		[DataRow("Object o = null;\n      Assert.IsFalse(o != null)", "Object o = null;\n      Assert.IsNull(o)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IsNullIsNotNull(string given, string expected)
		{
			VerifyChange(given, expected, expectedErrorLineOffset: 1);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true == false, \"blah\")", "Assert.AreEqual(true, false, \"blah\")")]
		[DataRow("Assert.IsTrue(true != false, \"blah\")", "Assert.AreNotEqual(true, false, \"blah\")")]
		[DataRow("Assert.IsFalse(true == false, \"blah\")", "Assert.AreNotEqual(true, false, \"blah\")")]
		[DataRow("Assert.IsFalse(true != false, \"blah\")", "Assert.AreEqual(true, false, \"blah\")")]
		[DataRow("Assert.IsFalse(true != false, \"blah\")", "Assert.AreEqual(true, false, \"blah\")")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void PreserveMessage(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true.Equals(false))", "Assert.AreEqual(true, false)")]
		//[DataRow("Assert.IsTrue(!true.Equals(false))", "Assert.AreNotEqual(true, false)")]
		[DataRow("Assert.IsFalse(true.Equals(false))", "Assert.AreNotEqual(true, false)")]
		[TestCategory(TestDefinitions.UnitTests)]
		//[DataRow("Assert.IsFalse(!true.Equals(false))", "Assert.AreEqual(true, false)")]
		//[DataRow("Assert.IsFalse(!true.Equals(false))", "Assert.AreEqual(true, false)")]
		public void ReplacesEquals(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true && true)", "Assert.IsTrue(true);\n      Assert.IsTrue(true)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CanBreakDownCompoundStatements(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true && true, \"blah\")", "Assert.IsTrue(true, \"blah\");\n      Assert.IsTrue(true, \"blah\")")]
		[DataRow("Assert.IsTrue(true && true, \"blah{0}\", 1)", "Assert.IsTrue(true, \"blah{0}\", 1);\n      Assert.IsTrue(true, \"blah{0}\", 1)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void PreserveCompoundMessages(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void StaticFunctionCallDoesntThrow()
		{
			string givenText = @"
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

			VerifySuccessfulCompilation(givenText);

			VerifyFix(givenText, givenText);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CommentsAreNotRemoved()
		{
			string given = @"
int i = 50;
      //test comment
      Assert.IsTrue(true && true)";

			string expected = @"
int i = 50;
      //test comment
      Assert.IsTrue(true);
      Assert.IsTrue(true)";

			VerifyChange(given, expected, 3);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void TrailingWhitespacePreserved()
		{
			string given = @"
int i = 50;
      //test comment
      Assert.IsTrue(true && true);

      i.ToString();
      int k = 4";

			string expected = @"
int i = 50;
      //test comment
      Assert.IsTrue(true);
      Assert.IsTrue(true);

      i.ToString();
      int k = 4";

			VerifyChange(given, expected, 3);
		}

		[DataTestMethod]
		[DataRow("Assert.IsFalse(1 != 2 && 2 == 3)")]
		[DataRow("Assert.IsTrue(1 != 2 || 2 == 3)")]
		[DataRow("Assert.IsFalse(1 != 2 || 2 == 3)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CanBreakDownFalseOrOrCompoundStatements(string given)
		{
			VerifyNoChange(given);
		}

		[DataTestMethod]
		[DataRow("bool value = false; Assert.IsFalse(value)")]
		[DataRow("Assert.IsFalse(\"foo\".Contains(\"f\"))")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EnsureWeDontChangeValues(string given)
		{
			VerifyNoChange(given);
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
		public void CheckLiteral(string given)
		{
			VerifyNoChange(given);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CheckNoCrash()
		{
			VerifyNoChange("Assert.IsTrue(");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CheckForNoSemanticModel()
		{
			const string template = @"
Assert.IsTrue(foo.Test());
";

			VerifyNoChange(template);
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AssertIsEqual),
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