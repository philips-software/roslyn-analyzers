// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Common
{
	/// <summary>
	/// Test class for types in <see cref="AllowedSymbols"/>.
	/// </summary>
	[TestClass]
	public class AllowedTypeSymbolsTest : DiagnosticVerifier
	{
		private const string AllowedSymbolsContent = @"
# Comment line
// Another comment line
; Another comment line
AllowedMethodName
*.TypeInAnyNamespace
*.*.MethodInAnyTypeAndNamespace
*.Log.*
ANamespace.*.MethodInAnyTypeInNamespace
Philips.Detailed.AType.AllowedMethodInFullNamespace
~N:AllowedNamespace # With comment on same line
~T:ANamespace.AllowedType ; With comment on same line
~M:ANamespace.AType.AllowedMethod() // With comment on same line
";

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AllowedSymbolsTestAnalyzer(false);
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return base.GetAdditionalTexts().Add(("NotFile.txt", "data")).Add((AllowedSymbolsTestAnalyzer.AllowedFileName, AllowedSymbolsContent));
		}

		[DataTestMethod]
		[DataRow("AllowedNamespace", "SomeType"),
		 DataRow("ANamespace", "AllowedType"),
		 DataRow("SomeNamespace", "TypeInAnyNamespace"),
		 DataRow("SomeNamespace", "AllowedMethodName"),
		 DataRow("Philips.Detailed.AType", "AllowedMethodInFullNamespace")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowedSymbolShouldBeReportDiagnostics(string nsName, string typeName)
		{
			string file = GenerateCodeFile(nsName, typeName);
			await VerifyAsync(file).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("SomeNamespace", "SomeType")]
		[DataRow("ANamespace", "AType")]
		[DataRow("SomeNamespace", "AllowedType")]
		[DataRow("AllowedMethodName", "AType")]
		[DataRow("ANamespace.DetailedNamespace", "AType")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NotAllowedSymbolShouldNotReportDiagnosticsAsync(string nsName, string typeName)
		{
			string file = GenerateCodeFile(nsName, typeName);
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		private string GenerateCodeFile(string nsName, string typeName)
		{
			return
				$"namespace {nsName} {{\npublic class {typeName}\n{{\n}}\n}}\n";
		}

		private async Task VerifyAsync(string file)
		{
			DiagnosticResult expected = new()
			{
				Id = AllowedSymbolsTestAnalyzer.Rule.Id,
				Message = new Regex("AllowedSymbolsFound"),
				Severity = DiagnosticSeverity.Error,
				Location = new DiagnosticResultLocation("Test0.cs", null, null)
			};

			await VerifyDiagnostic(file, expected).ConfigureAwait(false);
		}
	}
}
