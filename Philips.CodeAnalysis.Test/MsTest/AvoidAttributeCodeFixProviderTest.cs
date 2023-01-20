// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class AvoidAttributeCodeFixProviderTest : AssertCodeFixVerifier
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
		public void AvoidTestInitializeCodeFixProviderTest(string testMethod)
		{
			string givenText = string.Format(baseline, testMethod);

			var expected = GetExpectedDiagnostic(DiagnosticIds.AvoidTestInitializeMethod);
			VerifyCSharpDiagnostic(givenText, expected);

			VerifyFix(givenText, expectedText);
		}


		[DataTestMethod]
		[DataRow("[ClassInitialize]\n public void SomeMethod() {int i = 5;}")]
		public void AvoidClassInitializeCodeFixProviderTest(string testMethod)
		{
			string givenText = string.Format(baseline, testMethod);

			var expected = GetExpectedDiagnostic(DiagnosticIds.AvoidClassInitializeMethod);
			VerifyCSharpDiagnostic(givenText, expected);

			VerifyFix(givenText, expectedText);
		}

		[DataTestMethod]
		[DataRow("[TestCleanup]\n public void SomeMethod() {int i = 5;}")]
		public void AvoidTestCleanupCodeFixProviderTest(string testMethod)
		{
			string givenText = string.Format(baseline, testMethod);

			var expected = GetExpectedDiagnostic(DiagnosticIds.AvoidTestCleanupMethod);
			VerifyCSharpDiagnostic(givenText, expected);

			VerifyFix(givenText, expectedText);
		}

		[DataTestMethod]
		[DataRow("[ClassCleanup]\n public void SomeMethod() {int i = 5;}")]
		public void AvoidClassCleanupCodeFixProviderTest(string testMethod)
		{
			string givenText = string.Format(baseline, testMethod);

			var expected = GetExpectedDiagnostic(DiagnosticIds.AvoidClassCleanupMethod);
			VerifyCSharpDiagnostic(givenText, expected);

			VerifyFix(givenText, expectedText);
		}

		private DiagnosticResult GetExpectedDiagnostic(DiagnosticIds id)
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

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			throw new System.NotImplementedException();
		}
	}
}