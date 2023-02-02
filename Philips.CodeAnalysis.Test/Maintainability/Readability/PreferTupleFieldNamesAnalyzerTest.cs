// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.


using System;
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
	public class PreferTupleFieldNamesAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PreferTupleFieldNamesAnalyzer();
		}

		private string CreateFunction(string argument)
		{
			string baseline = @"
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

		#endregion

		#region Public Interface

		[DataRow("data.Item1", false)]
		[DataRow("data.Item2", true)]
		[DataRow("data.num", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NamedTuplesDontCauseErrors(string argument, bool isError)
		{
			var source = CreateFunction(argument);
			if (isError)
			{
				VerifyDiagnostic(source, DiagnosticResultHelper.Create(DiagnosticIds.PreferUsingNamedTupleField));
			}
			else
			{
				VerifySuccessfulCompilation(source);
			}
		}

		#endregion
	}
}
