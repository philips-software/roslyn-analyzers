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
	/// Test class for methods in <see cref="AllowedSymbols"/>.
	/// </summary>
	[TestClass]
	public class AllowedMethodSymbolsTest : DiagnosticVerifier
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
			return new AllowedSymbolsTestAnalyzer(true);
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return base.GetAdditionalTexts().Add(("NotFile.txt", "data")).Add((AllowedSymbolsTestAnalyzer.AllowedFileName, AllowedSymbolsContent));
		}

		[DataTestMethod]
		[DataRow("AllowedNamespace", "SomeType", "SomeMethod"),
		 DataRow("ANamespace", "AllowedType", "SomeMethod"),
		 DataRow("ANamespace", "AType", "AllowedMethod"),
		 DataRow("ANamespace", "SomeType", "AllowedMethodName"),
		 DataRow("NotListedNamespace", "TypeInAnyNamespace", "NotListedMethodName"),
		 DataRow("ANamespace", "NotListedType", "MethodInAnyTypeInNamespace"),
		 DataRow("NotListedNamespace", "NotListedType", "MethodInAnyTypeAndNamespace"),
		 DataRow("Philips.Detailed", "AType", "AllowedMethodInFullNamespace"),
		 DataRow("SomeNamespace", "Log", "SomeMethod")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AllowedSymbolShouldBeReportDiagnostics(string nsName, string typeName, string methodName)
		{
			var file = GenerateCodeFile(nsName, typeName, methodName);
			Verify(file);
		}

		[DataTestMethod]
		[DataRow("SomeNamespace", "SomeType", "SomeMethod")]
		[DataRow("ANamespace", "AType", "AllowedMethod2")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NotAllowedSymbolShouldNotReportDiagnostics(string nsName, string typeName, string methodName)
		{
			var file = GenerateCodeFile(nsName, typeName, methodName);
			VerifySuccessfulCompilation(file);
		}

		private string GenerateCodeFile(string nsName, string typeName, string methodName)
		{
			return
				$"namespace {nsName} {{\npublic class {typeName}\n{{\nprivate void {methodName}()\n{{\nreturn;\n}}\n}}\n}}\n";
		}

		private void Verify(string file)
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
