// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidAsyncVoidAnalyzerTest : AssertDiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow(true, "void")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTaskResultObjectInvalidCreationTestAsync(bool isAsync, string returnType)
		{
			string code = $@"using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Tests
{{
	public {(isAsync ? "async" : string.Empty)} {returnType} Foo() {{ throw new Exception(); }}
}}";

			await VerifyDiagnostic(code, DiagnosticId.AvoidAsyncVoid).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(false, "void")]
		[DataRow(true, "Task")]
		[DataRow(true, "Task<int>")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTaskResultObjectCreationValidTestAsync(bool isAsync, string returnType)
		{
			string code = $@"using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Tests
{{
	public {(isAsync ? "async" : string.Empty)} {returnType} Foo() {{ throw new Exception(); }}
}}";
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTaskResultObjectCreationCorrectTestAsync()
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

			await VerifySuccessfulCompilation(correctTemplate).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTaskResultObjectCreationInCorrectTestForCustomEventArgsAsync()
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

			await VerifyDiagnostic(correctTemplate, DiagnosticId.AvoidAsyncVoid).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTaskResultObjectCreationInCorrectTestForEventArgsAsync()
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

			await VerifySuccessfulCompilation(correctTemplate).ConfigureAwait(false);
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidAsyncVoidDelegateAsync(bool isError, string code)
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
				await VerifyDiagnostic(correctTemplate, DiagnosticId.AvoidAsyncVoid).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(correctTemplate).ConfigureAwait(false);
			}
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAsyncVoidAnalyzer();
		}
	}
}
