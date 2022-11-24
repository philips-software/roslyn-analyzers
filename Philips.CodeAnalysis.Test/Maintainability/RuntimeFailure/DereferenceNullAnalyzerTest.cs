using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	[TestClass]
	public class DereferenceNullAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new DereferenceNullAnalyzer();
		}

		private string GetTemplate()
		{
			return @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    {0};
    {1};
  }}
}}
";
		}
		
		[TestMethod]
		public void DereferenceNullAsExpressionWithNestedExpression()
		{
			string testCode = @"
class Foo 
{{
  public void Scan(MethodDefinition method, MethodData data)
		{

Instruction i = method.Body.Instructions[0];
			
				switch (i.OpCode.Code)
				{
					case Code.Call:
					case Code.Calli:
					case Code.Callvirt:
						MethodReference mr = i.Operand as MethodReference;

						MethodDefinition md = mr.Resolve();

						Scan(md, _locks[md]);


						break;
					default:
						break;
				}
			
		}
}}
";
			var expected = DiagnosticResultHelper.CreateArray(DiagnosticIds.DereferenceNull);
			VerifyCSharpDiagnostic(testCode, expected);
		}
		
		/// <summary>
		/// In this test, y is dereferenced after "y = obj as string" without first checking or re-assigning y.
		/// </summary>
		/// <param name="content1"></param>
		/// <param name="content2"></param>
		[DataTestMethod]
		[DataRow("string t0 = y.ToString()", "string t1 = y.ToString()")]
		[DataRow("", "string t1 = y.ToString()")]
		[DataRow("", "int t2 = y.Length")]
		[DataRow("string z = \"hi\"", "string t1 = y.ToString()")]
		[DataRow("string z = \"hi\"", "int t2 = y.Length")]
		public void DereferenceNullAsExpressionFindingTest(string content1, string content2)
		{
			string testCode = string.Format(GetTemplate(), content1, content2);
			var expected = DiagnosticResultHelper.CreateArray(DiagnosticIds.DereferenceNull);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// In this test, y is either read or written to before dereferencing.
		/// </summary>
		/// <param name="content1"></param>
		/// <param name="content2"></param>
		[DataTestMethod]
		[DataRow("", "")]
		[DataRow("y=string.Empty", "")]
		[DataRow("", "")]
		[DataRow("y=string.Empty", "int t2 = y.Length")]
		[DataRow("if (y==null) int b = 0", "int t2 = y.Length")]
		[DataRow("string z = \"hi\"", "int t2 = y?.Length")]
		public void DereferenceNullAsExpressionNoFindingTest(string content1, string content2)
		{
			string testCode = string.Format(GetTemplate(), content1, content2);
			VerifyCSharpDiagnostic(testCode, Array.Empty<DiagnosticResult>());
		}


		[TestMethod]
		public void DereferenceNullAsExpressionDifferentBlockTest()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    if (x.Length > 0)
    {{
      string t0 = y.ToString();
    }}
  }}
}}
";
			VerifyCSharpDiagnostic(testCode, Array.Empty<DiagnosticResult>());
		}


		[TestMethod]
		public void DereferenceNullAsExpressionIfDereferenceTest()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    if (y.ToString() == @"""")
    {{
       string t0 = y.ToString();
    }}
  }}
}}
";
			var expected = DiagnosticResultHelper.CreateArray(DiagnosticIds.DereferenceNull);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		[TestMethod]
		public void DereferenceNullAsExpressionIfCheckDereferenceTest()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    if (y != null && y.ToString() == @"")
    {{
       string t0 = y.ToString();
    }}
  }}
}}
";
			VerifyCSharpDiagnostic(testCode, Array.Empty<DiagnosticResult>());
		}

		[TestMethod]
		public void DereferenceNullAsExpressionIfCheckDereference2Test()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    if ((bool)(y?.ToString().Contains(""null"")))
    {{
       string t0 = y.ToString();
    }}
  }}
}}
";
			VerifyCSharpDiagnostic(testCode, Array.Empty<DiagnosticResult>());
		}


		[TestMethod]
		public void DereferenceNullAsExpressionWhileCheckDereferenceTest()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    while (y.ToString().Contains(""x""))
    {{
       string t0 = y.ToString();
    }}
  }}
}}
";
			var expected = DiagnosticResultHelper.CreateArray(DiagnosticIds.DereferenceNull);
			VerifyCSharpDiagnostic(testCode, expected);
		}


		[TestMethod]
		public void DereferenceNullAsExpressionWhileCheckDereference2Test()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    while (y != null && y.ToString().Contains(""x""))
    {{
       string t0 = y.ToString();
    }}
  }}
}}
";
			VerifyCSharpDiagnostic(testCode, Array.Empty<DiagnosticResult>());
		}

	}
}
