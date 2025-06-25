// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class TestMethodsShouldHaveValidReturnTypesAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestMethodsShouldHaveValidReturnTypesAnalyzer();
		}

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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MustReturnVoidAsync(bool isAsync, string returnType, bool isError)
		{
			var code = $@"using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[TestMethod]
	public {(isAsync ? "async" : string.Empty)} {returnType} Foo() {{ throw new Exception(); }}
}}";

			if (isError)
			{
				await VerifyDiagnostic(code, DiagnosticId.TestMethodsMustHaveValidReturnType).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(code).ConfigureAwait(false);
			}
		}

		#endregion
	}
}
