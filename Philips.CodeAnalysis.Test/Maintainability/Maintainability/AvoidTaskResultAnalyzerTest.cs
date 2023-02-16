// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidTaskResultAnalyzerTest : CodeFixVerifier
	{
		[DataRow("ValueTask", "4")]
		[DataRow("Task", "() => 4")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTaskResultTest(string taskType, string argument)
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
  public async void Foo()
  {{{{
    {taskType}<int> task = new {taskType}<int>({argument});
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"task.Result");
			string after = string.Format(template, @"await task");

			await VerifyDiagnostic(before, DiagnosticId.AvoidTaskResult).ConfigureAwait(false);
			await VerifyFix(before, after).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTaskResultObjectCreationTest()
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
  public async void Foo()
  {{{{
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"new Task<int>(() => 4).Result");
			string after = string.Format(template, @"await new Task<int>(() => 4)");

			await VerifyDiagnostic(before, DiagnosticId.AvoidTaskResult).ConfigureAwait(false);
			await VerifyFix(before, after).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTaskResultCallMethodTest()
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
  public async Task<int> Foo(int x)
  {{{{
    return new Task<int>(() => x);
  }}}}
  public async void MyTest()
  {{{{
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"Foo(1).Result");
			string after = string.Format(template, @"await Foo(1)");

			await VerifyDiagnostic(before, DiagnosticId.AvoidTaskResult).ConfigureAwait(false);
			await VerifyFix(before, after).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTaskResultCallMethodThisTest()
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
                   public async Task<int> Foo(int x)
  {{{{
    return new Task<int>(() => x);
  }}}}
  public async void MyTest()
  {{{{
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"this.Foo(1       ).Result");
			string after = string.Format(template, @"await this.Foo(1)");

			await VerifyDiagnostic(before, DiagnosticId.AvoidTaskResult).ConfigureAwait(false);
			await VerifyFix(before, after).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidTaskResultCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidTaskResultAnalyzer();
		}
	}
}
