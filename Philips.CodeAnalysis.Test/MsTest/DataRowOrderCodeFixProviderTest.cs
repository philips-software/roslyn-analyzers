// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class DataRowOrderCodeFixProviderTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DataRowOrderAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new DataRowOrderCodeFixProvider();
		}

		private static DiagnosticResult CreateExpectedResult(int line, int column)
		{
			return new DiagnosticResult
			{
				Id = DiagnosticId.DataRowOrderInTestMethod.ToId(),
				Severity = DiagnosticSeverity.Warning,
				Location = new DiagnosticResultLocation(null, line, column),
				Message = new System.Text.RegularExpressions.Regex(".*")
			};
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SimpleReorderingCodeFixAsync()
		{
			const string given = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[TestMethod]
	[DataRow(1, 2)]
	public void TestMethod1(int x, int y) { }
}";

			const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(1, 2)]
	[TestMethod]
	public void TestMethod1(int x, int y) { }
}";

			await VerifyDiagnostic(given, CreateExpectedResult(9, 14)).ConfigureAwait(false);
			await VerifyFix(given, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MultipleDataRowsCodeFixAsync()
		{
			const string given = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[TestMethod]
	[DataRow(1, 2)]
	[DataRow(3, 4)]
	public void TestMethod1(int x, int y) { }
}";

			const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(1, 2)]
	[DataRow(3, 4)]
	[TestMethod]
	public void TestMethod1(int x, int y) { }
}";

			await VerifyDiagnostic(given, CreateExpectedResult(10, 14)).ConfigureAwait(false);
			await VerifyFix(given, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WithOtherAttributesCodeFixAsync()
		{
			const string given = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[TestCategory(""Unit"")]
	[TestMethod]
	[DataRow(1, 2)]
	[Timeout(1000)]
	public void TestMethod1(int x, int y) { }
}";

			const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(1, 2)]
	[TestCategory(""Unit"")]
	[Timeout(1000)]
	[TestMethod]
	public void TestMethod1(int x, int y) { }
}";

			await VerifyDiagnostic(given, CreateExpectedResult(11, 14)).ConfigureAwait(false);
			await VerifyFix(given, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DataTestMethodCodeFixAsync()
		{
			const string given = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataTestMethod]
	[DataRow(""test"")]
	public void TestMethod1(string value) { }
}";

			const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(""test"")]
	[DataTestMethod]
	public void TestMethod1(string value) { }
}";

			await VerifyDiagnostic(given, CreateExpectedResult(9, 14)).ConfigureAwait(false);
			await VerifyFix(given, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ComplexMixedAttributesCodeFixAsync()
		{
			const string given = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[TestCategory(""Unit"")]
	[DataRow(1, 2)]
	[TestMethod]
	[DataRow(3, 4)]
	[Timeout(1000)]
	public void TestMethod1(int x, int y) { }
}";

			const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(1, 2)]
	[DataRow(3, 4)]
	[TestCategory(""Unit"")]
	[Timeout(1000)]
	[TestMethod]
	public void TestMethod1(int x, int y) { }
}";

			await VerifyDiagnostic(given, CreateExpectedResult(12, 14)).ConfigureAwait(false);
			await VerifyFix(given, expected).ConfigureAwait(false);
		}
	}
}