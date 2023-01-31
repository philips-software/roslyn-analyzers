// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Verifiers
{
	public abstract class AssertDiagnosticVerifier : DiagnosticVerifier
	{
		private readonly AssertCodeHelper _helper = new();

		protected void VerifyError(string methodBody, params string[] expectedDiagnosticIds)
		{
			var test = _helper.GetText(methodBody, string.Empty, string.Empty);

			var expected = expectedDiagnosticIds.Select(expectedDiagnosticId =>
				new DiagnosticResult()
				{
					Id = expectedDiagnosticId,
					Severity = DiagnosticSeverity.Error,
					Location = new DiagnosticResultLocation("Test0.cs", null, null),
				}).ToArray();
			VerifyDiagnostic(test, expected);
		}

		protected void VerifyNoError(string methodBody)
		{
			var test = _helper.GetText(methodBody, string.Empty, string.Empty);

			VerifySuccessfulCompilation(test);
		}



		protected override MetadataReference[] GetMetadataReferences()
		{
			return _helper.GetMetaDataReferences();
		}
	}
}
