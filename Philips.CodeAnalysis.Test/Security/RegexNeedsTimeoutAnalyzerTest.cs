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
	public class RegexNeedsTimeoutAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new RegexNeedsTimeoutAnalyzer();
		}

		protected override ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			string regexReference = typeof(Regex).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(regexReference);

			return base.GetMetadataReferences().Add(reference);
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
		// TODO: Figure out why implicit names are not found.
		//[DataRow(@"("".*"", RegexOptions.Compiled)")]
		//[DataRow(@"("".*"")")]
		[DataRow(@"Regex("".*"", RegexOptions.Compiled)")]
		[DataRow(@"Regex("".*"")")]
		[DataRow(@"System.Text.RegularExpressions.Regex("".*"", RegexOptions.Compiled)")]
		[DataRow(@"System.Text.RegularExpressions.Regex("".*"")")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WithoutTimeoutShouldTriggerDiagnosticAsync(string content)
		{
			string format = GetTemplate();
			string testCode = string.Format(format, content);
			await VerifyDiagnostic(testCode, DiagnosticId.RegexNeedsTimeout).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"Object()")]
		[DataRow(@"("".*"", RegexOptions.Compiled, TimeSpan.FromSeconds(1))")]
		[DataRow(@"Regex("".*"", RegexOptions.Compiled, TimeSpan.FromSeconds(1))")]
		[DataRow(@"Regex("".*"", RegexOptions.NonBacktracking)")]
		[DataRow(@"System.Text.RegularExpressions.Regex("".*"", RegexOptions.Compiled, TimeSpan.FromSeconds(1))")]
		[DataRow(@"System.Text.RegularExpressions.Regex("".*"", RegexOptions.NonBacktracking)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WithTimeoutShouldNotTriggerDiagnosticAsync(string content)
		{
			string format = GetTemplate();
			string testCode = string.Format(format, content);
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotTriggerDiagnosticInTestCodeAsync()
		{
			const string template = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo
{
	[TestMethod]
    public void Test()
	{
		Regex myRegex = new RegEx("".*"");
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);

		}

	}
}
