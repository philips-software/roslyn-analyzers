// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class EnforceFileVersionIsSameAsPackageVersionAnalyzerTest : DiagnosticVerifier
	{

		[DataTestMethod]
		[DataRow("1.0.1", "1.0.2", true)]
		[DataRow("1.1.0", "1.2.0", true)]
		[DataRow("1.0.0", "2.0.0", true)]
		[DataRow("1.0.0.1", "1.0.0.0", true)]
		[DataRow("1.0.0", "1.0.1-ci.1", true)]
		[DataRow("1.0.0", "1.0.1+417ce", true)]
		[DataRow("1.0.0", "1.0.1-beta+417ce", true)]
		[DataRow("1.0.0.0", "1.0.0", false)]
		[DataRow("1.0.0", "1.0.0.0", false)]
		[DataRow("1.0.0", "1.0.0", false)]
		[DataRow("1.0.0", "1.0.0-prerelease", false)]
		[DataRow("1.0.0", "1.0.0-ci.1", false)]
		[DataRow("1.1.2", "1.1.2+417ce", false)]
		[DataRow("1.1.2", "1.1.2-beta+417ce", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FileVersionMustBeSameAsPackageVersionAsync(string fileVersion, string packageVersion, bool hasDiagnostic)
		{
			var code = $@"
using System;
using System.Reflection;

[assembly: AssemblyVersion(""1.0.0.0"")]
[assembly: AssemblyFileVersion(""{fileVersion}"")]
[assembly: AssemblyInformationalVersion(""{packageVersion}"")]

class FooClass
{{{{
  public void Foo(object a)
  {{{{
    var data = 1;
  }}}}
}}}}
";

			if (hasDiagnostic)
			{
				await VerifyDiagnostic(code, DiagnosticId.EnforceFileVersionIsSameAsPackageVersion).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(code).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoDiagnosticWhenNoPackageVersionAsync()
		{
			var code = $@"
using System;
using System.Reflection;

[assembly: AssemblyVersion(""1.0.0.0"")]
[assembly: AssemblyFileVersion(""1.2.3.4"")]

class FooClass
{{{{
  public void Foo(object a)
  {{{{
    var data = 1;
  }}}}
}}}}
";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoDiagnosticWhenNoFileVersionOrPackageVersionAsync()
		{
			var code = $@"
using System;
using System.Reflection;

class FooClass
{{{{
  public void Foo(object a)
  {{{{
    var data = 1;
  }}}}
}}}}
";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceFileVersionIsSameAsPackageVersionAnalyzer();
		}
	}
}
