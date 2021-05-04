// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.


using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	/// <summary>
	/// 
	/// </summary>
	[TestClass]
	public class PreferNamedTuplesAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
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

		#endregion

		#region Public Interface

		[DataRow("(int Foo, int Bar)")]
		[DataTestMethod]
		public void NamedTuplesDontCauseErrors(string argument)
		{
			VerifyCSharpDiagnostic(CreateFunction(argument));
		}

		[DataRow("(int, int)")]
		[DataTestMethod]
		public void ErrorIfTupleElementsDoNotHaveNames(string argument)
		{
			VerifyCSharpDiagnostic(CreateFunction(argument), DiagnosticResultHelper.Create(DiagnosticIds.PreferTuplesWithNamedFields), DiagnosticResultHelper.Create(DiagnosticIds.PreferTuplesWithNamedFields));
		}

		[DataRow("(int Foo, int)")]
		[DataTestMethod]
		public void ErrorIfTupleElementDoesNotHaveName(string argument)
		{
			VerifyCSharpDiagnostic(CreateFunction(argument), DiagnosticResultHelper.Create(DiagnosticIds.PreferTuplesWithNamedFields));
		}

		#endregion
	}
}
