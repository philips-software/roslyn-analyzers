// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class TestMethodsShouldHaveValidReturnTypesAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new TestMethodsShouldHaveValidReturnTypesAnalyzer();

		#endregion

		#region Public Interface

		[DataRow(false, "void", false)]
		[DataRow(false, "int", true)]
		[DataRow(false, "Task", true)]
		[DataRow(false, "Task<int>", true)]
		[DataRow(true, "void", true)]
		[DataRow(true, "int", true)]
		[DataRow(true, "Task", false)]
		[DataRow(true, "Task<int>", true)]
		[DataTestMethod]
		public void TestMethodsMustReturnVoid(bool isAsync, string returnType, bool isError)
		{
			string code = $@"using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[TestMethod]
	public {(isAsync ? "async" : string.Empty)} {returnType} Foo() {{ throw new Exception(); }}
}}";

			VerifyCSharpDiagnostic(code, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.TestMethodsMustHaveValidReturnType) : Array.Empty<DiagnosticResult>());
		}

		#endregion
	}
}