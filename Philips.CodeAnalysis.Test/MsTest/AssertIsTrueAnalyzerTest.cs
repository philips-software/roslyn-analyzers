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
		public void ReplacesEqualsEquals(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Object o = null;\n      Assert.IsTrue(o == null)", "Object o = null;\n      Assert.IsNull(o)")]
		[DataRow("Object o = null;\n      Assert.IsTrue(o != null)", "Object o = null;\n      Assert.IsNotNull(o)")]
		[DataRow("Object o = null;\n      Assert.IsFalse(o == null)", "Object o = null;\n      Assert.IsNotNull(o)")]
		[DataRow("Object o = null;\n      Assert.IsFalse(o != null)", "Object o = null;\n      Assert.IsNull(o)")]
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
		public void PreserveMessage(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true.Equals(false))", "Assert.AreEqual(true, false)")]
		//[DataRow("Assert.IsTrue(!true.Equals(false))", "Assert.AreNotEqual(true, false)")]
		[DataRow("Assert.IsFalse(true.Equals(false))", "Assert.AreNotEqual(true, false)")]
		//[DataRow("Assert.IsFalse(!true.Equals(false))", "Assert.AreEqual(true, false)")]
		//[DataRow("Assert.IsFalse(!true.Equals(false))", "Assert.AreEqual(true, false)")]
		public void ReplacesEquals(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(1 != 2 && 2 == 3)", "Assert.IsTrue(1 != 2);\n      Assert.IsTrue(2 == 3)")]
		public void CanBreakDownCompoundStatements(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[DataTestMethod]
		[DataRow("Assert.IsTrue(1 != 2 && 2 == 3, \"blah\")", "Assert.IsTrue(1 != 2, \"blah\");\n      Assert.IsTrue(2 == 3, \"blah\")")]
		[DataRow("Assert.IsTrue(1 != 2 && 2 == 3, \"blah{0}\", 1)", "Assert.IsTrue(1 != 2, \"blah{0}\", 1);\n      Assert.IsTrue(2 == 3, \"blah{0}\", 1)")]
		public void PreserveCompoundMessages(string given, string expected)
		{
			VerifyChange(given, expected);
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(givenText);

			VerifyCSharpFix(givenText, givenText);
		}

		[TestMethod]
		public void CommentsAreNotRemoved()
		{
			string given = @"
int i = 50;
      //test comment
      Assert.IsTrue(1 != 2 && 2 == 3)";

			string expected = @"
int i = 50;
      //test comment
      Assert.IsTrue(1 != 2);
      Assert.IsTrue(2 == 3)";

			VerifyChange(given, expected, 3);
		}

		[TestMethod]
		public void TrailingWhitespacePreserved()
		{
			string given = @"
int i = 50;
      //test comment
      Assert.IsTrue(1 != 2 && 2 == 3);

      i.ToString();
      int k = 4";

			string expected = @"
int i = 50;
      //test comment
      Assert.IsTrue(1 != 2);
      Assert.IsTrue(2 == 3);

      i.ToString();
      int k = 4";

			VerifyChange(given, expected, 3);
		}

		[DataTestMethod]
		[DataRow("Assert.IsFalse(1 != 2 && 2 == 3)")]
		[DataRow("Assert.IsTrue(1 != 2 || 2 == 3)")]
		[DataRow("Assert.IsFalse(1 != 2 || 2 == 3)")]
		public void CanBreakDownFalseOrOrCompoundStatements(string given)
		{
			VerifyNoChange(given);
		}

		[DataTestMethod]
		[DataRow("bool value = false; Assert.IsFalse(value)")]
		[DataRow("Assert.IsFalse(\"foo\".Contains(\"f\"))")]
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
		public void CheckLiteral(string given)
		{
			VerifyNoChange(given);
		}

		[TestMethod]
		public void CheckNoCrash()
		{
			VerifyNoChange("Assert.IsTrue(");
		}

		[TestMethod]
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
				Id = Helper.ToDiagnosticId(DiagnosticIds.AssertIsEqual),
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