// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class EnforceEditorConfigAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceEditorConfigAnalyzer();
		}

		private const string TestCode = @"
class Foo 
{
}
";

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new []{(name: ".editorconfig", content: string.Empty)};
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HasEditorConfigShouldNotTriggerDiagnostics()
		{
			VerifySuccessfulCompilation(TestCode);
		}
	}
}
