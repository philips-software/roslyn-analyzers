// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.


using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class PreferNamedTuplesAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PreferNamedTuplesAnalyzer();
		}

		private string CreateFunction(string argument)
		{
			string baseline = @"
class Foo 
{{
  public void Foo({0} data)
  {{
  }}
}}
";

			return string.Format(baseline, argument);
		}


		[DataRow("(int Foo, int Bar)")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NamedTuplesDontCauseErrorsAsync(string argument)
		{
			var source = CreateFunction(argument);
			await VerifySuccessfulCompilation(source).ConfigureAwait(false);
		}

		[DataRow("(int, int)")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ErrorIfTupleElementsDoNotHaveNamesAsync(string argument)
		{
			var source = CreateFunction(argument);
			await VerifyDiagnostic(source, 2).ConfigureAwait(false);
		}

		[DataRow("(int Foo, int)")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ErrorIfTupleElementDoesNotHaveNameAsync(string argument)
		{
			var source = CreateFunction(argument);
			await VerifyDiagnostic(source).ConfigureAwait(false);
		}
	}
}
