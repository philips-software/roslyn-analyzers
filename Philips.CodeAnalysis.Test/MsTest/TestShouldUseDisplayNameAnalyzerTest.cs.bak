// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
	public class TestShouldUseDisplayNameAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TestShouldUseDisplayNameAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DataRowWithCommentButNoDisplayName_ShouldTriggerDiagnostic()
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
		public async Task DataRowWithDisplayName_ShouldNotTriggerDiagnostic()
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
		public async Task DataRowWithoutComment_ShouldNotTriggerDiagnostic()
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
		public async Task TestMethodWithCommentButNoDescription_ShouldTriggerDiagnostic()
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
		public async Task TestMethodWithDescription_ShouldNotTriggerDiagnostic()
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
		public async Task TestMethodWithoutComment_ShouldNotTriggerDiagnostic()
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
		public async Task TestMethodWithShortComment_ShouldNotTriggerDiagnostic()
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
		public async Task TestMethodWithBoilerplateComment_ShouldNotTriggerDiagnostic()
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
		public async Task MultipleDataRowsWithMixedComments_ShouldTriggerAppropriately()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[DataTestMethod]
	[DataRow(1, 2)] // Should add positive numbers
	[DataRow(-1, 1, DisplayName = ""Should handle negative numbers"")]
	[DataRow(0, 0)] // Should handle zero values
	public void TestAddition(int a, int b) { }
}";

			await VerifyDiagnostic(testCode, DiagnosticId.UseDisplayNameOrDescription, DiagnosticId.UseDisplayNameOrDescription).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodWithMultiLineComment_ShouldTriggerDiagnostic()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	/* This test verifies that addition works 
	   correctly for various inputs */
	[TestMethod]
	public void TestAddition() { }
}";

			await VerifyDiagnostic(testCode, DiagnosticId.UseDisplayNameOrDescription).ConfigureAwait(false);
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult
			{
				Id = DiagnosticId.UseDisplayNameOrDescription.ToId(),
				Message = new Regex(TestShouldUseDisplayNameAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Info,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 8 + expectedLineNumberErrorOffset, 13 + expectedColumnErrorOffset)
				}
			};
		}
	}
}