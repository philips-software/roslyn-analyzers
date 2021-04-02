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
		[TestMethod]
		public void AvoidTaskResultTest()
		{
			const string template = @"
using System;
class FooClass
{{
  public bool MyProperty {{ get; }}
  public FooClass Blah() {{ }}
  public async void Foo()
  {{
    {0};
  }}
}}
";
			string before = string.Format(template, @"Blah().MyProperty");
			string after = string.Format(template, @"await Blah()");

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
			analyzer.ContainingNamespace = string.Empty;
			analyzer.ContainingTypePrefix = @"FooClass";
			analyzer.Identifier = @"MyProperty";
			return analyzer;
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			throw new NotImplementedException();
		}
	}
}