// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class EnableDocumentationCreationAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnableDocumentationCreationAnalyzer();
		}

		private const string Correct = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
    }
}
";

		[DataTestMethod]
		[DataRow(Correct, DisplayName = nameof(Correct))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CorrectCodeShouldNotTriggerAnyDiagnosticsAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}
	}

	[TestClass]
	public class EnableDocumentationCreationAnalyzer2Test : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnableDocumentationCreationAnalyzer();
		}

		protected override ParseOptions GetParseOptions()
		{
			return new CSharpParseOptions(documentationMode: DocumentationMode.None);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task InvalidDocumentationModeTriggersDiagnostic()
		{
			await VerifyDiagnostic(@"").ConfigureAwait(false);
		}
	}

}
