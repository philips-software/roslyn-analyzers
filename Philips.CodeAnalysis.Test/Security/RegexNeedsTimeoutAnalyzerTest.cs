// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.SecurityAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Security
{
	[TestClass]
	public class RegexNeedsTimeoutAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new RegexNeedsTimeoutAnalyzer();
		}

		private string GetTemplate()
		{
			return @"
using System.Text.RegularExpressions;
namespace RegexNeedsTimeoutTest
{{
  public class Foo 
  {{
    public Regex MethodA()
    {{
      Regex myRegex = new {0};
      return myRegex;
    }}
  }}
}}
";
		}

		[DataTestMethod]
		// TODO: Figure out why fully qualified names are required.
		//[DataRow(@"("".*"", RegexOptions.Compiled)")]
		//[DataRow(@"("".*"")")]
		//[DataRow(@"Regex("".*"", RegexOptions.Compiled)")]
		//[DataRow(@"Regex("".*"")")]
		[DataRow(@"System.Text.RegularExpressions.Regex("".*"", RegexOptions.Compiled)")]
		[DataRow(@"System.Text.RegularExpressions.Regex("".*"")")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WithoutTimeoutShouldTriggerDiagnostic(string content)
		{
			var format = GetTemplate();
			string testCode = string.Format(format, content);
			var expected = DiagnosticResultHelper.Create(DiagnosticId.RegexNeedsTimeout);
			VerifyDiagnostic(testCode, expected);
		}

		[DataTestMethod]
		[DataRow(@"Regex("".*"", RegexOptions.Compiled, TimeSpan.FromSeconds(1))")]
		[DataRow(@"Regex("".*"", RegexOptions.NonBacktracking)")]
		[DataRow(@"System.Text.RegularExpressions.Regex("".*"", RegexOptions.Compiled, TimeSpan.FromSeconds(1))")]
		[DataRow(@"System.Text.RegularExpressions.Regex("".*"", RegexOptions.NonBacktracking)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WithTimeoutShouldNotTriggerDiagnostic(string content)
		{
			var format = GetTemplate();
			string testCode = string.Format(format, content);
			VerifySuccessfulCompilation(testCode);
		}
	}
}
