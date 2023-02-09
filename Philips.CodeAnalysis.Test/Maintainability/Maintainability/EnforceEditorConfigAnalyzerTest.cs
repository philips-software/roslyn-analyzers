// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
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

		protected override ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return base.GetAdditionalTexts().Add((name: ".editorconfig", content: string.Empty));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task HasEditorConfigShouldNotTriggerDiagnosticsAsync()
		{
			await VerifySuccessfulCompilation(TestCode).ConfigureAwait(false);
		}
	}
}
