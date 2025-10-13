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
		public async Task PreferCombinatorialTestingTriggersDiagnosticWithSingleParameterMethodAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(""value1"")]
	[DataRow(""value2"")]
	[DataRow(""value3"")]
	[DataRow(""value4"")]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithManyDataRows(string text)
	{
		// Test implementation
	}
}
";

			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.PreferCombinatorialTestingOverDataRows.ToId(),
				Message = new Regex(@"Use CombinatorialValues for this 1-parameter method with 4 DataRow attributes covering all parameter values."),
				Severity = DiagnosticSeverity.Info,
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 14) }
			};

			await VerifyDiagnostic(testCode, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingNoDiagnosticWithMultipleParametersAsync()
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
	public void TestMethodWithMultipleParameters(string text, int number)
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
		public async Task PreferCombinatorialTestingTriggersDiagnosticWithManySingleParameterDataRowsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(""private"")]
	[DataRow(""public"")]
	[DataRow(""internal"")]
	[DataRow(""protected"")]
	[DataRow(""static"")]
	[DataRow(""readonly"")]
	[DataRow(""const"")]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithManyDataRows(string modifier)
	{
		// Test implementation 
	}
}
";

			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.PreferCombinatorialTestingOverDataRows.ToId(),
				Message = new Regex(@"Use CombinatorialValues for this 1-parameter method with 7 DataRow attributes covering all parameter values."),
				Severity = DiagnosticSeverity.Info,
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 14) }
			};

			await VerifyDiagnostic(testCode, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingNoDiagnosticWithFewSingleParameterDataRowsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(""value1"")]
	[DataRow(""value2"")]
	[DataRow(""value3"")]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithFewDataRows(string text)
	{
		// Test implementation
	}
}
";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingNoDiagnosticWithDataRowsHavingMultipleArgumentsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(""value1"", 1)] // These DataRows have multiple arguments
	[DataRow(""value2"", 2)] // but method has single parameter  
	[DataRow(""value3"", 3)]
	[DataRow(""value4"", 4)]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithMismatchedDataRows(string text)
	{
		// Test implementation
	}
}
";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingTriggersDiagnosticWithTwoParametersFullCombinationsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(""A"", 1)]
	[DataRow(""A"", 2)]
	[DataRow(""B"", 1)]
	[DataRow(""B"", 2)]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithFullCombinations(string text, int number)
	{
		// Test implementation
	}
}
";

			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.PreferCombinatorialTestingOverDataRows.ToId(),
				Message = new Regex(@"Use CombinatorialValues for this 2-parameter method with 4 DataRow attributes covering all parameter combinations."),
				Severity = DiagnosticSeverity.Info,
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 14) }
			};

			await VerifyDiagnostic(testCode, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingNoDiagnosticWithTwoParametersIncompleteCombinationsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(""A"", 1)]
	[DataRow(""A"", 2)]
	[DataRow(""B"", 1)]
	// Missing [DataRow(""B"", 2)] for complete combinations
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithIncompleteCombinations(string text, int number)
	{
		// Test implementation
	}
}
";

			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreferCombinatorialTestingTriggersDiagnosticWithLargerTwoParameterCombinationsAsync()
		{
			var testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
	[TestMethod]
	[DataRow(true, ""small"")]
	[DataRow(true, ""medium"")]
	[DataRow(true, ""large"")]
	[DataRow(false, ""small"")]
	[DataRow(false, ""medium"")]
	[DataRow(false, ""large"")]
	[TestCategory(TestDefinitions.UnitTests)]
	public void TestMethodWithBooleanAndStringCombinations(bool enabled, string size)
	{
		// Test implementation
	}
}
";

			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.PreferCombinatorialTestingOverDataRows.ToId(),
				Message = new Regex(@"Use CombinatorialValues for this 2-parameter method with 6 DataRow attributes covering all parameter combinations."),
				Severity = DiagnosticSeverity.Info,
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 14) }
			};

			await VerifyDiagnostic(testCode, expected).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PreferCombinatorialTestingAnalyzer();
		}
	}
}