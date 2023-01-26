// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.SecurityAnalyzers;

namespace Philips.CodeAnalysis.Test.Security
{
	[TestClass]
	public class AvoidPasswordAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidPasswordAnalyzer();
		}

		private string GetTemplate()
		{
			return @"
class Foo 
{{
  {0}
  public void Foo()
  {{
    {1};
  }}
}}
";
		}

		[DataTestMethod]
		[DataRow("private string _x, _password);", @"")]
		[DataRow("private const string MyPassword = \"Hi\");", @"")]
		[DataRow("public string Password {get; set;}", @"")]
		[DataRow(@"", "/*  MyPassword */")]
		[DataRow(@"", "//  MyPassword")]
		public void CheckPasswordTest(string content0, string content1)
		{
			var format = GetTemplate();
			string testCode = string.Format(format, content0, content1);
			var expected = DiagnosticResultHelper.CreateArray(DiagnosticIds.AvoidPasswordField);
			VerifyDiagnostic(testCode, expected);
		}

		[DataTestMethod]
		[DataRow("private string _x, _y);", @"")]
		[DataRow("private const string MyField = \"Hi\");", @"")]
		[DataRow("public string MyProperty {get; set;}", @"")]
		[DataRow(@"", "/*  MyComment */")]
		[DataRow(@"", "//  MyComment")]
		public void CheckNoPasswordTest(string content0, string content1)
		{
			var format = GetTemplate();
			string testCode = string.Format(format, content0, content1);
			VerifySuccessfulCompilation(testCode);
		}
	}
}
