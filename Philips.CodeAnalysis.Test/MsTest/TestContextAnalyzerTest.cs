// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestContextAnalyzerTest : CodeFixVerifier
	{
		[TestMethod]
		public void HasTestContextPropertyButNoUsageTest()
		{
			string givenText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace TestContextAnalyzerTest
{
  public class TestClass
  {
    [TestMethod]
    public void TestMethod()
    {
    }

    public TestContext TestContext { get; }
  }
}
";

			DiagnosticResult[] expected = new [] { new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.TestContext),
				Message = new Regex(TestContextAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 15, 7)
				}
			}};
			bool runsOnNetFramework = RuntimeInformation.FrameworkDescription.Contains("Framework");
			VerifyCSharpDiagnostic(givenText, "Test0", runsOnNetFramework ? expected : Array.Empty<DiagnosticResult>());
		}
		
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TestContextAnalyzer();
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new TestContextCodeFixProvider();
		}
	}
}