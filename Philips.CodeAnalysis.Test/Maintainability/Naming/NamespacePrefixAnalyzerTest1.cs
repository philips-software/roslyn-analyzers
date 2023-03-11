// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class NamespacePrefixAnalyzerTest1 : DiagnosticVerifier
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

		private DiagnosticResultLocation GetBaseDiagnosticLocation(int rowOffset = 0, int columnOffset = 0)
		{
			return new DiagnosticResultLocation("Test.cs", 4 + rowOffset, 14 + columnOffset);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NamespacePrefixAnalyzer();
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ReportEmptyNamespacePrefix()
		{

			var code = string.Format(ClassString, "");
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
	}
}
