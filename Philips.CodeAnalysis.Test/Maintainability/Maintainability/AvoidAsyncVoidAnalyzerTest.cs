﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidAsyncVoidAnalyzerTest : AssertDiagnosticVerifier
	{

		[DataTestMethod]
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

			VerifyDiagnostic(code, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.AvoidAsyncVoid) : Array.Empty<DiagnosticResult>());
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

			VerifySuccessfulCompilation(correctTemplate);
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

			VerifyDiagnostic(correctTemplate, DiagnosticResultHelper.Create(DiagnosticIds.AvoidAsyncVoid));
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

			VerifySuccessfulCompilation(correctTemplate);
		}

		[DataRow(false, "Action<int> action = x => {{ Task.Yield(); return 4; }}")]
		[DataRow(false, "Action action = () => {{ Task.Yield(); }}")]
		[DataRow(true, "Action<int> action = async x => {{ await Task.Yield(); return 4; }}")]
		[DataRow(true, "Action action = async () => {{ await Task.Yield(); }}")]
		[DataRow(false, "Func<Task> action = async () => {{ await Task.Yield(); }}")]
		[DataRow(false, @"Task<Task<int>> t = Task.Factory.StartNew(async () =>
{
    await Task.Delay(1000);
    return 42;
});")]
		[DataTestMethod]
		public void AvoidAsyncVoidDelegate(bool isError, string code)
		{
			string correctTemplate = $@"
using System.Threading.Tasks;
using System;
class FooClass
{{
  public void Foo()
  {{
    {code};
  }}
}}
";
			if (isError)
			{
				VerifyDiagnostic(correctTemplate, DiagnosticResultHelper.Create(DiagnosticIds.AvoidAsyncVoid));
			}
			else
			{
				VerifySuccessfulCompilation(correctTemplate);
			}
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAsyncVoidAnalyzer();
		}
	}
}
