// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class EveryLinqStatementOnSeparateLineAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new EveryLinqStatementOnSeparateLineAnalyzer();
		}

		[TestMethod]
		public void SingleStatementsPerLineDoesNotTriggersDiagnostics()
		{
			string input = $@"
public static class Foo
{{
  public static void Method(int[] customers)
  {{
    var c = 
      from cust in customers
      where cust == 0
      select cust.ToString();
  }}
}}
";

			VerifyCSharpDiagnostic(input);
		}

		private const string WhereOnSameLine = $@"
public static class Foo
{{
  public static void Method(int[] customers)
  {{
    var c = 
      from cust in customers where cust == 0
      select cust.ToString();
  }}
}}
";

		private const string SelectOnSameLine = $@"
public static class Foo
{{
  public static void Method(int[] customers)
  {{
    var c = 
      from cust in customers
      where cust == 0 select cust.ToString();
  }}
}}
";

		[DataTestMethod]
		[DataRow(WhereOnSameLine, DisplayName = nameof(WhereOnSameLine)), 
		 DataRow(SelectOnSameLine, DisplayName = nameof(SelectOnSameLine))]
		public void MultipleStatementsOnSameLineTriggersDiagnostics(string input)
		{
			VerifyCSharpDiagnostic(input, DiagnosticResultHelper.Create(DiagnosticIds.EveryLinqStatementOnSeparateLine));
		}
		
	}
}
