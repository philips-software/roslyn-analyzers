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
		public void AvoidTaskResultObjectCreationTest()
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
  public async void Foo(object a, EventArgs b)
  {{{{
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"new Task<int>(() => 4).Result");
			string after = string.Format(template, @"await new Task<int>(() => 4)");

			VerifyCSharpDiagnostic(before, DiagnosticResultHelper.Create(DiagnosticIds.AvoidTaskVoid));
		}


		[TestMethod]
		public void AvoidTaskResultObjectCreationCorrectTest()
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
  public async Task Foo(object a, EventArgs b)
  {{{{
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"new Task<int>(() => 4).Result");
			string after = string.Format(template, @"await new Task<int>(() => 4)");

			VerifyCSharpDiagnostic(before, DiagnosticResultHelper.Create(DiagnosticIds.AvoidTaskVoid));
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
