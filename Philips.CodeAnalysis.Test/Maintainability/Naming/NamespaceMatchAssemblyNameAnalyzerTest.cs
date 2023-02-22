// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class NamespaceMatchAssemblyNameAnalyzerTest : DiagnosticVerifier
	{
		private const string Template = @"
namespace {0} {{
    public class Program {{
    }}
}}
";

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NamespaceMatchAssemblyNameAnalyzer();
		}

		[DataTestMethod]
		[DataRow("Philips.Test", "Philips.Production")]
		[DataRow("Philips.CodeAnalysis.Test", "Philips.CodeAnalysis.Production")]
		[DataRow("Philips.CodeAnalysis.Test", "Philips.CodeAnalysis.TestFramework")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability", "Philips.CodeAnalysis.Test.Maintainability.Foo")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability.Foo", "Philips.CodeAnalysis.Test.Maintainability.Foo.Bah")]
		[DataRow("Philips.CodeAnalysis.Test", "Philips.Test")]
		[DataRow("Philips.CodeAnalysis.Test", "CodeAnalysis.Test")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ReportIncorrectNamespaceMatchAsync(string ns, string assemblyName)
		{
			var code = string.Format(Template, ns);
			await VerifyDiagnostic(code, assemblyName: assemblyName).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("Philips.Test", "")]
		[DataRow("Philips.Test", "Philips.Test")]
		[DataRow("Philips.CodeAnalysis.Test", "Philips.CodeAnalysis")]
		[DataRow("System.Runtime.CompilerServices", "Philips.CodeAnalysis")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoNotReportANamespaceSupersetMatchAsync(string ns, string assemblyName)
		{
			var code = string.Format(Template, ns);
			await VerifySuccessfulCompilation(code, null, assemblyName).ConfigureAwait(false);
		}
	}
}
