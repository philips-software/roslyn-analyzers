// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

#pragma warning disable IDE0055, CA1707

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
	public class DataRowOrderAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DataRowOrderAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GoodOrderNoDiagnosticAsync()
		{
			const string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(1, 2)]
	[DataRow(3, 4)]
	[TestMethod]
	public void TestMethod1(int x, int y) { }

	[DataRow(""test"")]
	[DataTestMethod]
	public void TestMethod2(string value) { }
}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task BadOrderReportsDiagnosticAsync()
		{
			const string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[TestMethod]
	[DataRow(1, 2)]
	public void TestMethod1(int x, int y) { }
}";

			var expected = new DiagnosticResult
			{
				Id = DiagnosticId.DataRowOrderInTestMethod.ToId(),
				Severity = DiagnosticSeverity.Warning,
				Location = new DiagnosticResultLocation(null, 9, 14),
				Message = new System.Text.RegularExpressions.Regex(".*")
			};

			await VerifyDiagnostic(code, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoDataRowNoDiagnosticAsync()
		{
			const string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[TestMethod]
	public void TestMethod1() { }

	[DataTestMethod]
	public void TestMethod2() { }
}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoTestMethodNoDiagnosticAsync()
		{
			const string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(1, 2)]
	public void TestMethod1(int x, int y) { }
}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MixedWithOtherAttributesGoodOrderNoDiagnosticAsync()
		{
			const string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(1, 2)]
	[TestCategory(""Unit"")]
	[TestMethod]
	[Timeout(1000)]
	public void TestMethod1(int x, int y) { }
}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MixedWithOtherAttributesBadOrderReportsDiagnosticAsync()
		{
			const string code = @"
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

			var expected = new DiagnosticResult
			{
				Id = DiagnosticId.DataRowOrderInTestMethod.ToId(),
				Severity = DiagnosticSeverity.Warning,
				Location = new DiagnosticResultLocation(null, 11, 14),
				Message = new System.Text.RegularExpressions.Regex(".*")
			};

			await VerifyDiagnostic(code, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MultipleDataRowsGoodOrderNoDiagnosticAsync()
		{
			const string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(1, 2)]
	[DataRow(3, 4)]
	[DataRow(5, 6)]
	[TestMethod]
	public void TestMethod1(int x, int y) { }
}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MultipleDataRowsBadOrderReportsDiagnosticAsync()
		{
			const string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataRow(1, 2)]
	[TestMethod]
	[DataRow(3, 4)]
	public void TestMethod1(int x, int y) { }
}";

			var expected = new DiagnosticResult
			{
				Id = DiagnosticId.DataRowOrderInTestMethod.ToId(),
				Severity = DiagnosticSeverity.Warning,
				Location = new DiagnosticResultLocation(null, 10, 14),
				Message = new System.Text.RegularExpressions.Regex(".*")
			};

			await VerifyDiagnostic(code, expected).ConfigureAwait(false);
		}
	}
}