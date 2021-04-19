// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class AvoidRedundantSwitchStatementAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidRedundantSwitchStatementAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataRow("byte")]
		[DataRow("int")]
		[DataRow("string")]
		[DataTestMethod]
		public void SwitchWithOnlyDefaultCaseIsFlagged(string type)
		{
			string input = $@"
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

			VerifyCSharpDiagnostic(input, DiagnosticResultHelper.Create(DiagnosticIds.AvoidSwitchStatementsWithNoCases));
		}

		[DataRow("byte", "1")]
		[DataRow("int", "1")]
		[DataRow("string", "\"foo\"")]
		[DataTestMethod]
		public void SwitchWithMultipleCasesIsFlagged(string type, string value)
		{
			string input = $@"
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

			VerifyCSharpDiagnostic(input, DiagnosticResultHelper.Create(DiagnosticIds.AvoidSwitchStatementsWithNoCases));
		}

		[DataRow("byte", "1")]
		[DataRow("int", "1")]
		[DataRow("string", "\"foo\"")]
		[DataTestMethod]
		public void SwitchWithMultipleCasesIsIgnored(string type, string value)
		{
			string input = $@"
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

			VerifyCSharpDiagnostic(input);
		}


		[DataRow("byte")]
		[DataRow("int")]
		[DataRow("string")]
		[DataTestMethod]
		public void SwitchExpressionWithOnlyDefaultCaseIsFlagged(string type)
		{
			string input = $@"
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

			VerifyCSharpDiagnostic(input, DiagnosticResultHelper.Create(DiagnosticIds.AvoidSwitchStatementsWithNoCases));
		}

		[DataRow("byte", "1")]
		[DataRow("int", "1")]
		[DataRow("string", "\"foo\"")]
		[DataTestMethod]
		public void SwitchExpressionWithMultipleCasesIsIgnored(string type, string value)
		{
			string input = $@"
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

			VerifyCSharpDiagnostic(input);
		}

		#endregion
	}
}
