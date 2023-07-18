// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class PreferTupleFieldNamesAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PreferTupleFieldNamesAnalyzer();
		}

		private string CreateFunction(string argument)
		{
			var baseline = @"
class Foo 
{{
  public void Foo((string, int num) data)
  {{
    _ = {0};
  }}
}}
";

			return string.Format(baseline, argument);
		}

		[DataRow("data.Item1", false)]
		[DataRow("data.Item2", true)]
		[DataRow("data.num", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NamedTuplesDontCauseErrors(string argument, bool isError)
		{
			var source = CreateFunction(argument);
			if (isError)
			{
				await VerifyDiagnostic(source, DiagnosticId.PreferUsingNamedTupleField).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(source).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task StaticFieldsAreIgnored()
		{
			var givenText = @"
class Foo 
{{
  private static readonly (string, int num) data;
}}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoneTuplesAreIgnored()
		{
			var givenText = @"
class Foo 
{{
  private static readonly int num;
}}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}
	}
}
