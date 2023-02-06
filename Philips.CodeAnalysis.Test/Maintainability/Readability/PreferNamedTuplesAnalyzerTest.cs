// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.


using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	/// <summary>
	/// 
	/// </summary>
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
		public void NamedTuplesDontCauseErrors(string argument)
		{
			var source = CreateFunction(argument);
			VerifySuccessfulCompilation(source);
		}

		[DataRow("(int, int)")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ErrorIfTupleElementsDoNotHaveNames(string argument)
		{
			var source = CreateFunction(argument);
			VerifyDiagnostic(source, 2);
		}

		[DataRow("(int Foo, int)")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ErrorIfTupleElementDoesNotHaveName(string argument)
		{
			var source = CreateFunction(argument);
			VerifyDiagnostic(source);
		}
	}
}
