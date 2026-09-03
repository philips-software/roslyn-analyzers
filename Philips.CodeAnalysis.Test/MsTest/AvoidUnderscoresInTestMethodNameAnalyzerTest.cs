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
	public class AvoidUnderscoresInTestMethodNameAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidUnderscoresInTestMethodNameAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidUnderscoresInTestMethodNameCodeFixProvider();
		}

		[DataRow("Method_Scenario_Expected", false)]
		[DataRow("Method_Scenario", false)]
		[DataRow("Has_One_Underscore", false)]
		[DataRow("PascalCaseMethodName", true)]
		[DataRow("SimpleTest", true)]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UnderscoreInTestMethodName(string methodName, bool isCorrect)
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
				await VerifyDiagnostic(text, DiagnosticId.AvoidUnderscoresInTestMethodName).ConfigureAwait(false);
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
	public void Some_Test_Method() {{ }}
}}";

			var text = string.Format(code, attribute);
			await VerifyDiagnostic(text, DiagnosticId.AvoidUnderscoresInTestMethodName).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NonTestMethodWithUnderscoreIsIgnored()
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	public void Helper_Method() {{ }}

	[TestMethod]
	public void ValidTestName() {{ }}
}}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[DataRow("Method_Scenario_Expected", "MethodScenarioExpected")]
		[DataRow("Method_Scenario", "MethodScenario")]
		[DataRow("get_Something_Done", "GetSomethingDone")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixRemovesUnderscores(string original, string expected)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[TestMethod]
	public void {0}() {{ }}
}}";

			var givenText = string.Format(code, original);
			var fixedText = string.Format(code, expected);
			await VerifyFix(givenText, fixedText).ConfigureAwait(false);
		}
	}
}
