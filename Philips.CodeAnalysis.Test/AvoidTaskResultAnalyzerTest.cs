// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class AvoidTaskResultAnalyzerTest : AssertCodeFixVerifier
	{
		[DataRow("ValueTask")]
		[DataRow("Task")]
		[DataTestMethod]
		public void AvoidTaskResultTest(string taskType)
		{
			string template = $@"
using System;
using System.Threading.Tasks;
class FooClass
{{{{
  public async void Foo()
  {{{{
    {taskType}<int> task = new {taskType}<int>(() => 4);
    {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"task.Result");
			string after = string.Format(template, @"await task");

			VerifyCSharpDiagnostic(before, DiagnosticResultHelper.Create(DiagnosticIds.AvoidTaskResult));
			VerifyCSharpFix(before, after, null, allowNewCompilerDiagnostics: true);
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new AvoidTaskResultCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			var analyzer = new AvoidTaskResultAnalyzer();

			return analyzer;
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			throw new NotImplementedException();
		}
	}
}