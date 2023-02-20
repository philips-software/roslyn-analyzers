// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SingleStatementsPerLineDoesNotTriggersDiagnosticsAsync(string input)
		{

			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MultipleStatementsOnSameLineTriggersDiagnostics(string input)
		{
			await VerifyDiagnostic(input).ConfigureAwait(false);
			await VerifyFix(input, Correct).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(WhereOnSameLine, "Dummy.Designer", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggeredAsync(string testCode, string filePath)
		{
			await VerifySuccessfulCompilation(testCode, filePath).ConfigureAwait(false);
		}
	}
}
