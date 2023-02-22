// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class NamespacePrefixAnalyzerTest : DiagnosticVerifier
	{

		private const string ClassString = @"
			using System;
			using System.Globalization;
			namespace {0}Culture.Test
			{{
			class Foo 
			{{
				public void Foo()
				{{
				}}
			}}
			}}
			";

		private const string ConfiguredPrefix = @"Philips.iX";

		private DiagnosticResultLocation GetBaseDiagnosticLocation(int rowOffset = 0, int columnOffset = 0)
		{
			return new DiagnosticResultLocation("Test.cs", 4 + rowOffset, 14 + columnOffset);
		}


		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			return base.GetAdditionalAnalyzerConfigOptions().Add($@"dotnet_code_quality.{NamespacePrefixAnalyzer.RuleForIncorrectNamespace.Id}.namespace_prefix", ConfiguredPrefix);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NamespacePrefixAnalyzer();
		}

		[DataTestMethod]
		[DataRow("")]
		[DataRow("test")]
		[DataRow("Philips.Test")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ReportIncorrectNamespacePrefixAsync(string prefix)
		{

			var code = string.Format(ClassString, prefix);
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.NamespacePrefix),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation(0,0)
				}
			};

			await VerifyDiagnostic(code, expected).ConfigureAwait(false);
		}

		[DataRow(ConfiguredPrefix + ".")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoNotReportANamespacePrefixErrorAsync(string ns)
		{
			var code = string.Format(ClassString, ns);
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[DataRow("System.Runtime.CompilerServices")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoNotReportANamespaceOnExemptListAsync(string ns)
		{
			var template = @"namespace {0} {{ class Foo {{ }} }}";
			var code = string.Format(template, ns);
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}
	}
}
