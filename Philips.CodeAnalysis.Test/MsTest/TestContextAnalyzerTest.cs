// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Serialization;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestContextAnalyzerTest : CodeFixVerifier
	{
		protected override MetadataReference[] GetMetadataReferences()
		{
			string testContextReference = typeof(TestContext).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(testContextReference);
			return base.GetMetadataReferences().Concat(new[] { reference }).ToArray();
		}

		[TestMethod]
		public void HasTestContextPropertyButNoUsageTest()
		{
			string givenText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace TestContextAnalyzerTest
{
  public class TestClass
  {
    private string x = ""5"";
    [TestMethod]
    public void TestMethod()
    {
    }

    public TestContext TestContext { get {return x;} }
  }
}
";

string fixedText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace TestContextAnalyzerTest
{
  public class TestClass
  {
    [TestMethod]
    public void TestMethod()
    {
    }
  }
}
";

			VerifyCSharpDiagnostic(givenText, DiagnosticResultHelper.Create(DiagnosticIds.TestContext));
			VerifyFix(givenText, fixedText);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestContextAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new TestContextCodeFixProvider();
		}
	}
}