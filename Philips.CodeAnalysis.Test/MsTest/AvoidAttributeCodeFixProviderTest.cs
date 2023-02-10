// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
	public class AvoidAttributeCodeFixProviderTest : CodeFixVerifier
	{
		private readonly string baseline = @"
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            [TestClass]
            class Foo 
            {{
              {0}
            }}
            ";

		private readonly string expectedText = @"
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            [TestClass]
            class Foo 
            {
            }
            ";

		[DataTestMethod]
		[DataRow("[TestInitialize]\n public void SomeMethod() {int i = 5;}")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTestInitializeCodeFixProviderTest(string testMethod)
		{
			string givenText = string.Format(baseline, testMethod);

			var expected = GetExpectedDiagnostic(DiagnosticId.AvoidTestInitializeMethod);
			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);

			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow("[ClassInitialize]\n public void SomeMethod() {int i = 5;}")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidClassInitializeCodeFixProviderTest(string testMethod)
		{
			string givenText = string.Format(baseline, testMethod);

			var expected = GetExpectedDiagnostic(DiagnosticId.AvoidClassInitializeMethod);
			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);

			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("[TestCleanup]\n public void SomeMethod() {int i = 5;}")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTestCleanupCodeFixProviderTest(string testMethod)
		{
			string givenText = string.Format(baseline, testMethod);

			var expected = GetExpectedDiagnostic(DiagnosticId.AvoidTestCleanupMethod);
			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);

			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("[ClassCleanup]\n public void SomeMethod() {int i = 5;}")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidClassCleanupCodeFixProviderTest(string testMethod)
		{
			string givenText = string.Format(baseline, testMethod);

			var expected = GetExpectedDiagnostic(DiagnosticId.AvoidClassCleanupMethod);
			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);

			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		private DiagnosticResult GetExpectedDiagnostic(DiagnosticId id)
		{
			return new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(id),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 6, 16)
				}
			};
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidMethodsCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAttributeAnalyzer();
		}
	}
}