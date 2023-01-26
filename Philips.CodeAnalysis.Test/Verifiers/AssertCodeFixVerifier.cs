// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.Test
{
	public abstract class AssertCodeFixVerifier : CodeFixVerifier
	{
		private readonly AssertCodeHelper _helper = new();

		protected string OtherClassSyntax { get; set; } = string.Empty;
		protected string DefaultMethodAttributes { get; set; } = string.Empty;

		protected abstract DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0);


		protected void VerifyNoChange(string methodBody)
		{
			VerifyNoChange(methodBody, DefaultMethodAttributes);
		}

		protected void VerifyNoChange(string methodBody, string methodAttributes)
		{
			var test = _helper.GetText(methodBody, OtherClassSyntax, methodAttributes);

			VerifySuccessfulCompilation(test);

			var fixtest = _helper.GetText(methodBody, OtherClassSyntax, methodAttributes);

			VerifyFix(test, fixtest);
		}

		protected void VerifyChange(string methodBody, string expectedBody, int expectedErrorLineOffset = 0, int expectedErrorColumnOffset = 0, bool allowNewCompilerDiagnostics = false)
		{
			VerifyChange(methodBody, expectedBody, DefaultMethodAttributes, DefaultMethodAttributes, expectedErrorLineOffset, expectedErrorColumnOffset, allowNewCompilerDiagnostics);
		}

		protected void VerifyChange(string methodBody, string expectedBody, string methodAttributes, string expectedAttributes, int expectedErrorLineOffset = 0, int expectedErrorColumnOffset = 0, bool allowNewCompilerDiagnostics = false)
		{
			var test = _helper.GetText(methodBody, OtherClassSyntax, methodAttributes);
			var expected = GetExpectedDiagnostic(expectedLineNumberErrorOffset: expectedErrorLineOffset, expectedColumnErrorOffset: expectedErrorColumnOffset);

			VerifyDiagnostic(test, expected);

			var fixtest = _helper.GetText(expectedBody, OtherClassSyntax, expectedAttributes);

			VerifyFix(test, fixtest, null, allowNewCompilerDiagnostics);
		}

		protected void VerifyError(string methodBody, string methodAttributes, int expectedErrorLineOffset = 0, int expectedErrorColumnOffset = 0, string error = null)
		{
			var test = _helper.GetText(methodBody, OtherClassSyntax, methodAttributes);
			var expected = GetExpectedDiagnostic(expectedLineNumberErrorOffset: expectedErrorLineOffset, expectedColumnErrorOffset: expectedErrorColumnOffset);

			if (error != null)
			{
				expected.Message = new Regex(error);
			}

			VerifyDiagnostic(test, expected);
		}

		protected void VerifyError(string methodBody, int expectedErrorLineOffset = 0, int expectedErrorColumnOffset = 0, string error = null)
		{
			VerifyError(methodBody, string.Empty, expectedErrorLineOffset, expectedErrorColumnOffset, error);
		}

		protected void VerifyNoError(string methodBody)
		{
			var test = _helper.GetText(methodBody, OtherClassSyntax, string.Empty);

			VerifySuccessfulCompilation(test);
		}

		protected override MetadataReference[] GetMetadataReferences()
		{
			return _helper.GetMetaDataReferences();
		}
	}
}
