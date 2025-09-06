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
	public class PreferCombinatorialTestingAnalyzerTest : DiagnosticVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingTriggersDiagnosticWithMultipleDataRowsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(""value1"", 1)]
	[DataRow(""value2"", 2)]
	[DataRow(""value3"", 3)]
	[DataRow(""value4"", 4)]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithManyDataRows(string text, int number)
	{
		// Test implementation
	}
}
";

			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.PreferCombinatorialTestingOverDataRows.ToId(),
				Message = new Regex(@"Consider using combinatorial testing instead of multiple DataRow attributes. This method has 4 DataRow attributes."),
				Severity = DiagnosticSeverity.Info,
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 14) }
			};

			await VerifyDiagnostic(testCode, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingNoDiagnosticWithFewDataRowsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(""value1"", 1)]
	[DataRow(""value2"", 2)]
	[DataRow(""value3"", 3)]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithFewDataRows(string text, int number)
	{
		// Test implementation
	}
}
";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingNoDiagnosticWithNoDataRowsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithoutDataRows()
	{
		// Test implementation
	}
}
";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingTriggersDiagnosticWithManyDataRowsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(""private static readonly"", false)]
	[DataRow(""private static"", false)]
	[DataRow(""private const"", false)]
	[DataRow(""private"", false)]
	[DataRow(""public static readonly"", true)]
	[DataRow(""public const"", true)]
	[DataRow(""public static"", true)]
	[DataRow(""public readonly"", true)]
	[DataRow(""public"", true)]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithManyDataRows(string modifiers, bool expected)
	{
		// Test implementation similar to HelperTest example
	}
}
";

			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.PreferCombinatorialTestingOverDataRows.ToId(),
				Message = new Regex(@"Consider using combinatorial testing instead of multiple DataRow attributes. This method has 9 DataRow attributes."),
				Severity = DiagnosticSeverity.Info,
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 14) }
			};

			await VerifyDiagnostic(testCode, expected).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PreferCombinatorialTestingAnalyzer();
		}
	}
}