// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidRedundantSwitchStatementAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidRedundantSwitchStatementAnalyzer();
		}

		[DataRow("byte")]
		[DataRow("int")]
		[DataRow("string")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SwitchWithOnlyDefaultCaseIsFlagged(string type)
		{
			var input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    switch(data)
    {{
      default:
        System.Console.WriteLine(data);
        break;
    }}
  }}
}}
";

			await VerifyDiagnostic(input, DiagnosticId.AvoidSwitchStatementsWithNoCases).ConfigureAwait(false);
		}


		private const string SampleMethodWithSwitches = @"
public class Foo
{
  public void Method(int data)
  {
    switch(data)
    {
      default:
        System.Console.WriteLine(data);
        break;
    }
    int a = data switch
    {
      _ => 1
    }
  }
}
";

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedSwitchWithOnlyDefaultCaseIsNotFlagged()
		{
			var input = @"[System.CodeDom.Compiler.GeneratedCodeAttribute(""protoc"", null)]" + SampleMethodWithSwitches;
			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedFileSwitchWithOnlyDefaultCaseIsNotFlagged()
		{
			await VerifySuccessfulCompilation(SampleMethodWithSwitches, @"Foo.designer").ConfigureAwait(false);
		}

		[DataRow("byte", "1")]
		[DataRow("int", "1")]
		[DataRow("string", "\"foo\"")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SwitchWithMultipleCasesIsFlagged(string type, string value)
		{
			var input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    switch(data)
    {{
      case {value}:
      default:
        System.Console.WriteLine(data);
        break;
    }}
  }}
}}
";

			await VerifyDiagnostic(input, DiagnosticId.AvoidSwitchStatementsWithNoCases).ConfigureAwait(false);
		}

		[DataRow("byte", "1")]
		[DataRow("int", "1")]
		[DataRow("string", "\"foo\"")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SwitchWithMultipleCasesIsIgnored(string type, string value)
		{
			var input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    switch(data)
    {{
      case {value}:
        break;
      default:
        System.Console.WriteLine(data);
        break;
    }}
  }}
}}
";

			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}


		[DataRow("byte")]
		[DataRow("int")]
		[DataRow("string")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SwitchExpressionWithOnlyDefaultCaseIsFlagged(string type)
		{
			var input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    int a = data switch
    {{
      _ => 1
    }}
  }}
}}
";

			await VerifyDiagnostic(input, DiagnosticId.AvoidSwitchStatementsWithNoCases).ConfigureAwait(false);
		}

		[DataRow("byte", "1")]
		[DataRow("int", "1")]
		[DataRow("string", "\"foo\"")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SwitchExpressionWithMultipleCasesIsIgnored(string type, string value)
		{
			var input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    int a = data switch
    {{
      {value} => 2,
      _ => 1
    }}
  }}
}}
";

			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}

	}

	[TestClass]
	public class AvoidRedundantSwitchStatementAnalyzerGeneratedCodeTest : AvoidRedundantSwitchStatementAnalyzerTest
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidRedundantSwitchStatementAnalyzer(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		}
	}
}
