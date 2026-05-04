// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class PreferAssertHasCountAnalyzerTest : AssertCodeFixVerifier
	{
		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult()
			{
				Id = DiagnosticId.PreferAssertHasCount.ToId(),
				Location = new DiagnosticResultLocation("Test0.cs", null, null),
				Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Info,
			};
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PreferAssertHasCountAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new PreferAssertHasCountCodeFixProvider();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagsAreEqualWithTwoCountArguments()
		{
			var body = @"
var expected = new System.Collections.Generic.List<int>();
var actual = new System.Collections.Generic.List<int>();
Assert.AreEqual(expected.Count, actual.Count);
";
			var expected = @"
var expected = new System.Collections.Generic.List<int>();
var actual = new System.Collections.Generic.List<int>();
Assert.HasCount(expected.Count, actual);
";
			await VerifyChange(body, expected, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagsAreEqualWithNestedCountArguments()
		{
			var body = @"
var a = new { Foo = new System.Collections.Generic.List<int>() };
var b = new { Bar = new System.Collections.Generic.List<int>() };
Assert.AreEqual(a.Foo.Count, b.Bar.Count);
";
			var expected = @"
var a = new { Foo = new System.Collections.Generic.List<int>() };
var b = new { Bar = new System.Collections.Generic.List<int>() };
Assert.HasCount(a.Foo.Count, b.Bar);
";
			await VerifyChange(body, expected, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotFlagWhenSecondArgumentIsNotCountMemberAccess()
		{
			var body = @"
var expected = new System.Collections.Generic.List<int>();
int actualCount = 0;
Assert.AreEqual(expected.Count, actualCount);
";
			await VerifyNoError(body).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotFlagWhenFirstArgumentIsNotCountMemberAccess()
		{
			var body = @"
int expectedCount = 0;
var actual = new System.Collections.Generic.List<int>();
Assert.AreEqual(expectedCount, actual.Count);
";
			await VerifyNoError(body).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreservesTrailingMessageArgument()
		{
			var body = @"
var expected = new System.Collections.Generic.List<int>();
var actual = new System.Collections.Generic.List<int>();
Assert.AreEqual(expected.Count, actual.Count, ""message"");
";
			var fixedBody = @"
var expected = new System.Collections.Generic.List<int>();
var actual = new System.Collections.Generic.List<int>();
Assert.HasCount(expected.Count, actual, ""message"");
";
			await VerifyChange(body, fixedBody, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}
	}
}
