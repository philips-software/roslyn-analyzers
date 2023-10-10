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
	public class TestClassMustBePublicAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestClassMustBePublicAnalyzer();
		}

		[DataRow("", false)]
		[DataRow("static", false)]
		[DataRow("public static", true)]
		[DataRow("private static", false)]
		[DataRow("private", false)]
		[DataRow("protected", false)]
		[DataRow("public", true)]
		[DataRow("sealed", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestClassMustBePublicAsync(string modifier, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
{1} class Tests
{{
	{0}
	public void Foo() {{ }}
}}";

			foreach (var testType in new[] { "[TestMethod]", "[DataTestMethod]" })
			{
				var text = string.Format(code, testType, modifier);

				if (isCorrect)
				{
					await VerifySuccessfulCompilation(text).ConfigureAwait(false);
				}
				else
				{
					await VerifyDiagnostic(text, new DiagnosticResult()
					{
						Id = DiagnosticId.TestClassesMustBePublic.ToId(),
						Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, modifier.Length + 8) },
						Message = new Regex(".*"),
						Severity = DiagnosticSeverity.Error,
					}).ConfigureAwait(false);
				}
			}
		}

		[DataRow("", false)]
		[DataRow("static", false)]
		[DataRow("public static", true)]
		[DataRow("private static", false)]
		[DataRow("private", false)]
		[DataRow("protected", false)]
		[DataRow("protected static", false)]
		[DataRow("public", true)]
		[DataRow("sealed", false)]
		[DataRow("public sealed", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestClassWithAssemblyInitializeMustBePublicStaticAsync(string modifier, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
{1} class Tests
{{
	{0}
	public static void Foo() {{ }}
}}";

			foreach (var testType in new[] { "[AssemblyInitialize]", "[AssemblyCleanup]" })
			{
				var text = string.Format(code, testType, modifier);

				if (isCorrect)
				{
					await VerifySuccessfulCompilation(text).ConfigureAwait(false);
				}
				else
				{
					await VerifyDiagnostic(text, DiagnosticId.TestClassesMustBePublic);
				}
			}
		}
	}
}
