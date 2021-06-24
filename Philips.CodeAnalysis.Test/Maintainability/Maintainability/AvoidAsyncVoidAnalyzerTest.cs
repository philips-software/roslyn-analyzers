using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidAsyncVoidAnalyzerTest : AssertCodeFixVerifier
	{

		[TestMethod]
		[DataRow(false, "void", false)]
		[DataRow(true, "void", true)]
		[DataRow(true, "Task", false)]
		[DataRow(true, "Task<int>", false)]
		public void AvoidTaskResultObjectCreationTest(bool isAsync, string returnType, bool isError)
		{
			string code = $@"using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Tests
{{
	public {(isAsync ? "async" : string.Empty)} {returnType} Foo() {{ throw new Exception(); }}
}}";

			VerifyCSharpDiagnostic(code, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.AvoidAsyncVoid) : Array.Empty<DiagnosticResult>());
		}


		[TestMethod]
		public void AvoidTaskResultObjectCreationCorrectTest()
		{
			string correctTemplate = $@"
using System.Threading.Tasks;
using System;
class FooClass
{{{{
  public async void Foo(object a, MyEventArgs b)
  {{{{
    var data = 1;
  }}}}
}}}}

public class MyEventArgs : EventArgs
{{{{
    private string m_Data;
    public MyEventArgs(string _myData)
    {{{{
        m_Data = _myData;
    }}}}
    public string Data {{{{get{{{{return m_Data}}}} }}}}
}}}}
";

			VerifyCSharpDiagnostic(correctTemplate, Array.Empty<DiagnosticResult>());
		}

		[TestMethod]
		public void AvoidTaskResultObjectCreationInCorrectTestForCustomEventArgs()
		{
			string correctTemplate = $@"
using System.Threading.Tasks;
using System;
class FooClass
{{{{
  public async void Foo(object a, MyEventArgs b)
  {{{{
    var data = 1;
  }}}}
}}}}
";

			VerifyCSharpDiagnostic(correctTemplate, DiagnosticResultHelper.Create(DiagnosticIds.AvoidAsyncVoid));
		}

		[TestMethod]
		public void AvoidTaskResultObjectCreationInCorrectTestForEventArgs()
		{
			string correctTemplate = $@"
using System.Threading.Tasks;
using System;
class FooClass
{{{{
  public async void Foo(object a, EventArgs b)
  {{{{
    var data = 1;
  }}}}
}}}}
";

			VerifyCSharpDiagnostic(correctTemplate, Array.Empty<DiagnosticResult>());
		}


		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			throw new NotImplementedException();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidAsyncVoidAnalyzer();
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			throw new NotImplementedException();
		}
	}
}
