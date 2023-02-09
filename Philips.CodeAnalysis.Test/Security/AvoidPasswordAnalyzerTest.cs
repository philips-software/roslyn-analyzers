// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.SecurityAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckPasswordTestAsync(string content0, string content1)
		{
			var format = GetTemplate();
			string testCode = string.Format(format, content0, content1);
			await VerifyDiagnostic(testCode, DiagnosticId.AvoidPasswordField).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("private string _x, _y);", @"")]
		[DataRow("private const string MyField = \"Hi\");", @"")]
		[DataRow("public string MyProperty {get; set;}", @"")]
		[DataRow(@"", "/*  MyComment */")]
		[DataRow(@"", "//  MyComment")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckNoPasswordTestAsync(string content0, string content1)
		{
			var format = GetTemplate();
			string testCode = string.Format(format, content0, content1);
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}
	}
}
