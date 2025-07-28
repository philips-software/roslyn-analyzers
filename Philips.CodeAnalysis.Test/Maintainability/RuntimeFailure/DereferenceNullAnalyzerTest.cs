// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
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
		public async Task DereferenceNullAsExpressionWithNestedExpressionAsync()
		{
			var testCode = @"
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
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
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
		public async Task DereferenceNullAsExpressionFindingTestAsync(string content1, string content2)
		{
			var format = GetTemplate();
			var testCode = string.Format(format, content1, content2);
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// In this test, y is either read or written to before dereferencing.
		/// </summary>
		/// <param name="content1"></param>
		/// <param name="content2"></param>
		[DataTestMethod]
		[DataRow("", "")]
		[DataRow("y=string.Empty", "")]
		[DataRow("y=string.Empty", "int t2 = y.Length")]
		[DataRow("if (y==null) int b = 0", "int t2 = y.Length")]
		[DataRow("string z = \"hi\"", "int t2 = y?.Length")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionNoFindingTestAsync(string content1, string content2)
		{
			var format = GetTemplate();
			var testCode = string.Format(format, content1, content2);
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionDifferentBlockTestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionIfDereferenceTestAsync()
		{
			var testCode = @"
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
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionIfCheckDereferenceTestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionReturnLogicalAndCheckDereferenceTestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionIfCheckLogicalOrDereferenceTestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionReturnCheckLogicalOrDereferenceTestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionIfCheckDereference2TestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionIfCheckDereference3TestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionIfCheckDereference4TestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionIfCheckHasValueTestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionWhileCheckDereferenceTestAsync()
		{
			var testCode = @"
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
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionWhileCheckDereference2TestAsync()
		{
			var testCode = @"
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
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DereferenceNullAsExpressionPropertyCheckNotVariableCheckTestAsync()
		{
			var testCode = @"
class DataGridViewTextBoxCell 
{{
  public object Value {{ get; set; }}
  public void Foo()
  {{
    object obj = new DataGridViewTextBoxCell();
    DataGridViewTextBoxCell assignedCell = obj as DataGridViewTextBoxCell;
    if (assignedCell.Value == null)
    {{
      assignedCell.Value = string.Empty;
    }}
  }}
}}
";
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
    }
    
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MultipleAsExpressionsInSameBlockShouldNotTriggerFalsePositiveAsync()
		{
			var testCode = @"
class SomeKindOfModel
{{
	public bool IsValid {{ get; set; }}
}}

class Foo 
{{
	public void TestMethod(object aModel, object anOldModel)
	{{
		var model = aModel as SomeKindOfModel;
		var oldModel = anOldModel as SomeKindOfModel;
		if (oldModel != null && oldModel.IsValid) {{
			// some code
		}}            
		if (model != null && model.IsValid) {{
			// some code
		}}
	}}
}}
";
			// This should not produce any diagnostics since both variables are properly null-checked
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}
	}
}
