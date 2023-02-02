// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
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

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new [] { ("NotFile.txt", "data"), (AllowedSymbolsTestAnalyzer.AllowedFileName, AllowedSymbolsContent) };
		}

		[DataTestMethod]
		[DataRow("AllowedNamespace", "SomeType"),
		 DataRow("ANamespace", "AllowedType"),
		 DataRow("SomeNamespace", "TypeInAnyNamespace"),
		 DataRow("SomeNamespace", "AllowedMethodName"),
		 DataRow("Philips.Detailed.AType", "AllowedMethodInFullNamespace")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AllowedSymbolShouldBeReportDiagnostics(string nsName, string typeName)
		{
			var file = GenerateCodeFile(nsName, typeName);
			VerifyDiagnostic(file);
		}

		[DataTestMethod]
		[DataRow("SomeNamespace", "SomeType")]
		[DataRow("ANamespace", "AType")]
		[DataRow("SomeNamespace", "AllowedType")]
		[DataRow("AllowedMethodName", "AType")]
		[DataRow("ANamespace.DetailedNamespace", "AType")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NotAllowedSymbolShouldNotReportDiagnostics(string nsName, string typeName)
		{
			var file = GenerateCodeFile(nsName, typeName);
			VerifySuccessfulCompilation(file);
		}

		private string GenerateCodeFile(string nsName, string typeName)
		{
			return
				$"namespace {nsName} {{\npublic class {typeName}\n{{\n}}\n}}\n";
		}

		private void VerifyDiagnostic(string file)
		{
			VerifyDiagnostic(file,
				new DiagnosticResult()
				{
					Id = AllowedSymbolsTestAnalyzer.Rule.Id,
					Message = new Regex("AllowedSymbolsFound"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", null, null)
					}
				}
			);
		}
	}
}
