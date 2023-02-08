// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Verifiers
{
	public abstract class AssertDiagnosticVerifier : DiagnosticVerifier
	{
		private readonly AssertCodeHelper _helper = new();

		protected async Task VerifyError(string methodBody, string expectedDiagnosticId)
		{
			var test = _helper.GetText(methodBody, string.Empty, string.Empty);

			var expected = 
				new DiagnosticResult()
				{
					Id = expectedDiagnosticId,
					Severity = DiagnosticSeverity.Error,
					Location = new DiagnosticResultLocation("Test0.cs", null, null),
				};
			await VerifyDiagnostic(test, expected).ConfigureAwait(false);
		}

		protected async Task VerifyNoError(string methodBody)
		{
			var test = _helper.GetText(methodBody, string.Empty, string.Empty);

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}
	}
}
