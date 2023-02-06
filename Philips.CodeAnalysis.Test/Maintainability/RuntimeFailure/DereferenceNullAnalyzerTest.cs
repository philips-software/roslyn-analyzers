// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	[TestClass]
	public class DereferenceNullAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DereferenceNullAnalyzer();
		}

		private static string GetTemplate()
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
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifyDiagnostic(testCode);
		}
		
		/// <summary>
		/// In this test, y is de-referenced after "y = obj as string" without first checking or re-assigning y.
		/// </summary>
		/// <param name="content1"></param>
		/// <param name="content2"></param>
		[DataTestMethod]
		[DataRow("string t0 = y.ToString()", "string t1 = y.ToString()")]
		[DataRow("", "string t1 = y.ToString()")]
		[DataRow("", "int t2 = y.Length")]
		[DataRow("string z = \"hi\"", "string t1 = y.ToString()")]
		[DataRow("string z = \"hi\"", "int t2 = y.Length")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DereferenceNullAsExpressionFindingTest(string content1, string content2)
		{
			var format = GetTemplate();
			string testCode = string.Format(format, content1, content2);
			VerifyDiagnostic(testCode);
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
		[TestCategory(TestDefinitions.UnitTests)]
		public void DereferenceNullAsExpressionNoFindingTest(string content1, string content2)
		{
			var format = GetTemplate();
			string testCode = string.Format(format, content1, content2);
			VerifySuccessfulCompilation(testCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifySuccessfulCompilation(testCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifyDiagnostic(testCode);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifySuccessfulCompilation(testCode);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DereferenceNullAsExpressionReturnLogicalAndCheckDereferenceTest()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    return (y != null && y.ToString() == @"");
  }}
}}
";
			VerifySuccessfulCompilation(testCode);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DereferenceNullAsExpressionIfCheckLogicalOrDereferenceTest()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    if (y == null || y.ToString() == @"""")
    {{
       string t0 = ""hi"";
    }}
  }}
}}
";
			VerifySuccessfulCompilation(testCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DereferenceNullAsExpressionReturnCheckLogicalOrDereferenceTest()
		{
			string testCode = @"
class Foo 
{{
  public bool Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    return y == null || y.ToString() == @"";
  }}
}}
";
			VerifySuccessfulCompilation(testCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifySuccessfulCompilation(testCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DereferenceNullAsExpressionIfCheckDereference3Test()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    string t0 = y != null ? y.ToString() : ""null"";
  }}
}}
";
			VerifySuccessfulCompilation(testCode);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DereferenceNullAsExpressionIfCheckDereference4Test()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string y = obj as string;
    if (x == ""SomeThing"" && y != null)
    {{
      string t2 = y.ToString();
    }}
  }}
}}
";
			VerifySuccessfulCompilation(testCode);
		}
		
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DereferenceNullAsExpressionIfCheckHasValueTest()
		{
			string testCode = @"
class Foo 
{{
  public void Foo()
  {{
    string x = String.Empty;
    object obj = x;
    string? y = obj as string;
    if (y.HasValue)
	{{
		string t0 = y.ToString();
    }}
  }}
}}
";
			VerifySuccessfulCompilation(testCode);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifyDiagnostic(testCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifySuccessfulCompilation(testCode);
		}

	}
}
