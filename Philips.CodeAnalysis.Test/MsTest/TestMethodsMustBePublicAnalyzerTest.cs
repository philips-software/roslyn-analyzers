// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class TestMethodsMustBePublicAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestMethodsMustBePublicAnalyzer();
		}

		[DataRow("", false)]
		[DataRow("static", false)]
		[DataRow("public static", false)]
		[DataRow("private static", false)]
		[DataRow("private", false)]
		[DataRow("protected", false)]
		[DataRow("public", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodsMustBeInTestClassAsync(string modifier, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	{0}
	{1} void Foo() {{ }}
}}";

			foreach (string testType in new[] { "[TestMethod]", "[DataTestMethod]" })
			{
				string text = string.Format(code, testType, modifier);

				if (isCorrect)
				{
					await VerifySuccessfulCompilation(text).ConfigureAwait(false);
				}
				else
				{
					await VerifyDiagnostic(text, DiagnosticId.TestMethodsMustBePublic).ConfigureAwait(false);
				}
			}
		}
	}
}
