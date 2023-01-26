// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

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
		public void FileVersionMustBeSameAsPackageVersion(string fileVersion, string packageVersion, bool hasDiagnostic)
		{
			string code = $@"
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
				VerifyDiagnostic(code, DiagnosticResultHelper.Create(DiagnosticIds.EnforceFileVersionIsSameAsPackageVersion));
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[TestMethod]
		public void NoDiagnosticWhenNoPackageVersion()
		{
			string code = $@"
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

			VerifySuccessfulCompilation(code);
		}

		[TestMethod]
		public void NoDiagnosticWhenNoFileVersionOrPackageVersion()
		{
			string code = $@"
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

			VerifySuccessfulCompilation(code);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceFileVersionIsSameAsPackageVersionAnalyzer();
		}
	}
}
