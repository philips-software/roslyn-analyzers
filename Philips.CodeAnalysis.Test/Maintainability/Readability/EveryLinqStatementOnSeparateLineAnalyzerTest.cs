// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class EveryLinqStatementOnSeparateLineAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EveryLinqStatementOnSeparateLineAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new EveryLinqStatementOnSeparateLineCodeFixProvider();
		}
		
		private const string Correct = $@"
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

		private const string CorrectWithComments = $@"
public static class Foo
{{
  public static void Method(int[] customers)
  {{
    var c = 
      from cust in customers // Get customers
      where cust == 0 // Which have not bought anything
      select cust.ToString(); // And report them
  }}
}}
";

		private const string CorrectWithCommentsOnSeparateLine = $@"
public static class Foo
{{
  public static void Method(int[] customers)
  {{
    var c = 
      // Get customers
      from cust in customers 
      // Which have not bought anything
      where cust == 0 
      // And report them
      select cust.ToString();
  }}
}}
";

		[DataTestMethod]
		[DataRow(Correct, DisplayName = nameof(Correct)),
		 DataRow(CorrectWithComments, DisplayName = nameof(CorrectWithComments)),
		 DataRow(CorrectWithCommentsOnSeparateLine, DisplayName = nameof(CorrectWithCommentsOnSeparateLine))]
		public void SingleStatementsPerLineDoesNotTriggersDiagnostics(string input)
		{

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
			VerifyCSharpFix(input, Correct);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow(WhereOnSameLine, "Dummy.Designer", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyDiagnostic(testCode, filePath);
		}
	}
}
