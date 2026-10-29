// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class SortMembersIntoRegionsTest : CodeFixVerifier
	{
		[TestMethod]
		[DataRow("private int _a;", "Non-Public Data Members")]
		[DataRow("int _a;", "Non-Public Data Members")]
		[DataRow("protected internal int _a;", "Non-Public Data Members")]
		[DataRow("private enum E { X }", "Non-Public Data Members")]
		[DataRow("private delegate void D();", "Non-Public Data Members")]
		[DataRow("private void M() { }", "Non-Public Properties/Methods")]
		[DataRow("protected int P { get; set; }", "Non-Public Properties/Methods")]
		[DataRow("internal Foo() { }", "Non-Public Properties/Methods")]
		[DataRow("static Foo() { }", "Non-Public Properties/Methods")]
		[DataRow("private event System.EventHandler E;", "Non-Public Properties/Methods")]
		[DataRow("private class Nested { }", "Non-Public Properties/Methods")]
		[DataRow("public int _a;", "Public Interface")]
		[DataRow("public void M() { }", "Public Interface")]
		[DataRow("public int P { get; set; }", "Public Interface")]
		[DataRow("public class Nested { }", "Public Interface")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MemberInExpectedRegionIsAccepted(string member, string region)
		{
			var givenText = $@"
class Foo
{{
	#region {region}
	{member}
	#endregion
}}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[DataRow("[TestMethod] public void TestA() { }", "Test Methods")]
		[DataRow("[DataTestMethod] public void TestA() { }", "Test Methods")]
		[DataRow("[Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute] public void TestA() { }", "Test Methods")]
		[DataRow("private class FooMock { }", "Test Mocks")]
		[DataRow("internal class MockFoo { }", "Test Mocks")]
		[DataRow("public class FooMock { }", "Test Classes")]
		[DataRow("private class Helper { }", "Test Classes")]
		[DataRow("[TestInitialize] public void Init() { }", "Public Interface")]
		[DataRow("private int _a;", "Non-Public Data Members")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestClassMemberInExpectedRegionIsAccepted(string member, string region)
		{
			var givenText = $@"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FooTests
{{
	#region {region}
	{member}
	#endregion
}}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyClassIsAccepted()
		{
			await VerifySuccessfulCompilation("class Foo { }").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NestedCustomRegionInsideExpectedRegionIsAccepted()
		{
			var givenText = @"
class Foo
{
	#region Public Interface
	#region Helpers
	public void A() { }
	#endregion
	#endregion
}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NestedTypesAreNotAnalyzed()
		{
			var givenText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FooTests
{
	#region Test Mocks
	private class FooMock
	{
		public void A() { }
		private int _b;
	}
	#endregion
}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ConditionalDirectivesBetweenMembersAreSkipped()
		{
			var givenText = @"
class Foo
{
	public void A() { }
#if DEBUG
	private int _b;
#endif
}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UnsortedClassIsSortedIntoRegions()
		{
			var givenText = @"
class Foo
{
	public void A() { }
	private int _b;
	private void C() { }
}
";
			var fixedText = @"
class Foo
{
	#region Non-Public Data Members

	private int _b;

	#endregion

	#region Non-Public Properties/Methods

	private void C() { }

	#endregion

	#region Public Interface

	public void A() { }

	#endregion
}
";
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MemberInWrongRegionIsMoved()
		{
			var givenText = @"
struct Foo
{
	#region Public Interface
	private int _b;
	#endregion

	#region Non-Public Data Members
	public void A() { }
	#endregion
}
";
			var fixedText = @"
struct Foo
{
	#region Non-Public Data Members

	private int _b;

	#endregion

	#region Public Interface

	public void A() { }

	#endregion
}
";
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CommentsAndDocumentationMoveWithMember()
		{
			var givenText = @"
class Foo
{
	public void A() { }

	/// <summary>Doc</summary>
	// Comment
	private int _b;
}
";
			var fixedText = @"
class Foo
{
	#region Non-Public Data Members

	/// <summary>Doc</summary>
	// Comment
	private int _b;

	#endregion

	#region Public Interface

	public void A() { }

	#endregion
}
";
			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldsAreNotSeparatedButMethodsAre()
		{
			var givenText = @"
class Foo
{
	private int _a;

	private void M() { }

	private int _b;


	private int _c;
	private void N() { }
	public int D;
	public int E;
	public void F() { }
}
";
			var fixedText = @"
class Foo
{
	#region Non-Public Data Members

	private int _a;
	private int _b;
	private int _c;

	#endregion

	#region Non-Public Properties/Methods

	private void M() { }

	private void N() { }

	#endregion

	#region Public Interface

	public int D;
	public int E;

	public void F() { }

	#endregion
}
";
			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestClassIsSortedIntoTestRegions()
		{
			var givenText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FooTests
{
	private class FooMock { }
	[TestMethod]
	public void TestA() { }
	public class Helper { }
	private int _x;
	[TestInitialize]
	public void Init() { }
}
";
			var fixedText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FooTests
{
	#region Test Mocks

	private class FooMock { }

	#endregion

	#region Non-Public Data Members

	private int _x;

	#endregion

	#region Public Interface

	[TestInitialize]
	public void Init() { }

	#endregion

	#region Test Methods

	[TestMethod]
	public void TestA() { }

	#endregion

	#region Test Classes

	public class Helper { }

	#endregion
}
";
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SingleLineClassIsSorted()
		{
			var givenText = @"
record Foo { public void A() { } private int _b; }
";
			var fixedText = @"
record Foo {
	#region Non-Public Data Members

	private int _b;

	#endregion

	#region Public Interface

	public void A() { }

	#endregion
}
";
			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FixAllSortsEveryType()
		{
			var givenText = @"
class Foo
{
	public void A() { }
	private int _b;
}

class Bar
{
	public void A() { }
	private int _b;
}
";
			var fixedText = @"
class Foo
{
	#region Non-Public Data Members

	private int _b;

	#endregion

	#region Public Interface

	public void A() { }

	#endregion
}

class Bar
{
	#region Non-Public Data Members

	private int _b;

	#endregion

	#region Public Interface

	public void A() { }

	#endregion
}
";
			await VerifyFixAll(givenText, fixedText).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new SortMembersIntoRegionsAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new SortMembersIntoRegionsCodeFixProvider();
		}
	}
}
