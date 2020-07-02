// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class DataTestMethodsHaveDataRowsAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new DataTestMethodsHaveDataRowsAnalyzer();
		}

		#endregion

		#region Public Interface

		[TestMethod]
		public void DataTestMethodsMustHaveDataRows()
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataTestMethod]
	public void Foo() { }
}";

			VerifyCSharpDiagnostic(code, new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.DataTestMethodsHaveDataRows),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, null) }
			});
		}

		[DataRow("DataRow(\"arg\")")]
		[DataRow("DynamicData(\"test\")")]
		[DataTestMethod]
		public void DataTestMethodsMustHaveDataRows2(string arg)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[{0}]
	[DataTestMethod]
	public void Foo() {{ }}
}}";

			VerifyCSharpDiagnostic(string.Format(code, arg));
		}

		[DataRow("DataRow(\"arg\")")]
		[DataRow("DynamicData(\"test\")")]
		[DataTestMethod]
		public void TestMethodsMustNotHaveDataRows(string arg)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[{0}]
	[TestMethod]
	public void Foo() {{ }}
}}";

			VerifyCSharpDiagnostic(string.Format(code, arg), DiagnosticResultHelper.Create(DiagnosticIds.DataTestMethodsHaveDataRows));
		}

		#endregion
	}
}
