// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Verifiers
{
	public abstract class AssertCodeFixVerifier : CodeFixVerifier
	{
		private readonly AssertCodeHelper _helper = new();

		protected string OtherClassSyntax { get; set; } = string.Empty;
		protected string DefaultMethodAttributes { get; set; } = string.Empty;

		protected abstract DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0);


		protected async Task VerifyNoChange(string methodBody)
		{
			await VerifyNoChange(methodBody, DefaultMethodAttributes).ConfigureAwait(false);
		}

		protected async Task VerifyNoChange(string methodBody, string methodAttributes)
		{
			var test = _helper.GetText(methodBody, OtherClassSyntax, methodAttributes);

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);

			var fixtest = _helper.GetText(methodBody, OtherClassSyntax, methodAttributes);

			await VerifyFix(test, fixtest).ConfigureAwait(false);
		}

		protected async Task VerifyChange(string methodBody, string expectedBody, int expectedErrorLineOffset = 0, int expectedErrorColumnOffset = 0, bool shouldAllowNewCompilerDiagnostics = false)
		{
			await VerifyChange(methodBody, expectedBody, DefaultMethodAttributes, DefaultMethodAttributes, expectedErrorLineOffset, expectedErrorColumnOffset, shouldAllowNewCompilerDiagnostics).ConfigureAwait(false);
		}

		protected async Task VerifyChange(string methodBody, string expectedBody, string methodAttributes, string expectedAttributes, int expectedErrorLineOffset = 0, int expectedErrorColumnOffset = 0, bool shouldAllowNewCompilerDiagnostics = false)
		{
			var test = _helper.GetText(methodBody, OtherClassSyntax, methodAttributes);
			var expected = GetExpectedDiagnostic(expectedLineNumberErrorOffset: expectedErrorLineOffset, expectedColumnErrorOffset: expectedErrorColumnOffset);

			await VerifyDiagnostic(test, expected).ConfigureAwait(false);

			var fixtest = _helper.GetText(expectedBody, OtherClassSyntax, expectedAttributes);

			await VerifyFix(test, fixtest, null, shouldAllowNewCompilerDiagnostics).ConfigureAwait(false);
		}

		protected async Task VerifyError(string methodBody, string methodAttributes, int expectedErrorLineOffset = 0, int expectedErrorColumnOffset = 0, string error = null)
		{
			var test = _helper.GetText(methodBody, OtherClassSyntax, methodAttributes);
			var expected = GetExpectedDiagnostic(expectedLineNumberErrorOffset: expectedErrorLineOffset, expectedColumnErrorOffset: expectedErrorColumnOffset);

			if (error != null)
			{
				expected.Message = new Regex(error, RegexOptions.Singleline, TimeSpan.FromSeconds(1));
			}

			await VerifyDiagnostic(test, expected).ConfigureAwait(false);
		}

		protected async Task VerifyErrorAsync(string methodBody, int expectedErrorLineOffset = 0, int expectedErrorColumnOffset = 0, string error = null)
		{
			await VerifyError(methodBody, string.Empty, expectedErrorLineOffset, expectedErrorColumnOffset, error).ConfigureAwait(false);
		}

		protected async Task VerifyNoError(string methodBody)
		{
			var test = _helper.GetText(methodBody, OtherClassSyntax, string.Empty);

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}
	}
}
