// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class EnforceUnderscoresInTestMethodNameAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceUnderscoresInTestMethodNameAnalyzer();
		}

		[DataRow("PascalCaseMethodName", false)]
		[DataRow("SimpleTest", false)]
		[DataRow("Method_Scenario_Expected", true)]
		[DataRow("Method_Scenario", true)]
		[DataRow("Has_One_Underscore", true)]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UnderscoreEnforcedInTestMethodName(string methodName, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[TestMethod]
	public void {0}() {{ }}
}}";

			var text = string.Format(code, methodName);

			if (isCorrect)
			{
				await VerifySuccessfulCompilation(text).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(text, DiagnosticId.EnforceUnderscoresInTestMethodName).ConfigureAwait(false);
			}
		}

		[DataRow("DataTestMethod")]
		[DataRow("STATestMethod")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WorksWithDerivedTestMethodAttributes(string attribute)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[{0}]
	public void PascalCaseOnly() {{ }}
}}";

			var text = string.Format(code, attribute);
			await VerifyDiagnostic(text, DiagnosticId.EnforceUnderscoresInTestMethodName).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NonTestMethodWithoutUnderscoreIsIgnored()
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	public void HelperMethod() {{ }}

	[TestMethod]
	public void Valid_Test_Name() {{ }}
}}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}
	}
}
