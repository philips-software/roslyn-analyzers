// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.


using System.Linq;
using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.Test
{
	public abstract class AssertDiagnosticVerifier : DiagnosticVerifier
	{
		#region Non-Public Data Members

		private readonly AssertCodeHelper _helper = new();

		#endregion

		#region Non-Public Properties/Methods

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
			VerifyCSharpDiagnostic(test, expected);
		}

		protected void VerifyNoError(string methodBody)
		{
			var test = _helper.GetText(methodBody, string.Empty, string.Empty);

			VerifyCSharpDiagnostic(test);
		}

		#endregion

		#region Public Interface


		protected override MetadataReference[] GetMetadataReferences()
		{
			return _helper.GetMetaDataReferences();
		}

		#endregion
	}
}
