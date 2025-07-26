// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestShouldUseDisplayNameAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestShouldUseDisplayNameAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DataRowWithCommentButNoDisplayNameShouldTriggerDiagnostic()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[DataTestMethod]
	[DataRow(1, 2)] // Should add numbers correctly
	public void TestAddition(int a, int b) { }
}";

			await VerifyDiagnostic(testCode, DiagnosticId.UseDisplayNameOrDescription).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DataRowWithDisplayNameShouldNotTriggerDiagnostic()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[DataTestMethod]
	[DataRow(1, 2, DisplayName = ""Should add numbers correctly"")]
	public void TestAddition(int a, int b) { }
}";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DataRowWithoutCommentShouldNotTriggerDiagnostic()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[DataTestMethod]
	[DataRow(1, 2)]
	public void TestAddition(int a, int b) { }
}";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodWithCommentButNoDescriptionShouldTriggerDiagnostic()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	// This test verifies that addition works correctly
	[TestMethod]
	public void TestAddition() { }
}";

			await VerifyDiagnostic(testCode, DiagnosticId.UseDisplayNameOrDescription).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodWithDescriptionShouldNotTriggerDiagnostic()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[Description(""This test verifies that addition works correctly"")]
	public void TestAddition() { }
}";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodWithoutCommentShouldNotTriggerDiagnostic()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	public void TestAddition() { }
}";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodWithShortCommentShouldNotTriggerDiagnostic()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	// Test
	[TestMethod]
	public void TestAddition() { }
}";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodWithBoilerplateCommentShouldNotTriggerDiagnostic()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	// TODO: implement this test
	[TestMethod]
	public void TestAddition() { }
}";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MultipleDataRowsWithCommentsShouldTriggerDiagnostics()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[DataTestMethod]
	[DataRow(1, 2)] // Should add positive numbers
	[DataRow(0, 0)] // Should handle zero values
	public void TestAddition(int a, int b) { }
}";

			await VerifyDiagnostic(testCode, DiagnosticId.UseDisplayNameOrDescription).ConfigureAwait(false);
		}
	}
}
