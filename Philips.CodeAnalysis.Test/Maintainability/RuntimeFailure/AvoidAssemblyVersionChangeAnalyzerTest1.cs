// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	[TestClass]
	public class AvoidAssemblyVersionChangeAnalyzerTest1 : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestableAvoidAssemblyVersionChangeAnalyzer(WrongReturnedVersion);
		}

		private const string TestCode = @"
class Foo 
{
}
";

		private const string ConfiguredVersion = "1.2.3.4";
		private const string WrongReturnedVersion = "2.0.0.0";

		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			return base.GetAdditionalAnalyzerConfigOptions().Add($@"dotnet_code_quality.{Helper.ToDiagnosticId(DiagnosticId.AvoidAssemblyVersionChange)}.assembly_version", ConfiguredVersion);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task HasWrongVersionTriggersDiagnosticsAsync()
		{
			await VerifyDiagnostic(TestCode, DiagnosticId.AvoidAssemblyVersionChange).ConfigureAwait(false);
		}
	}
}
