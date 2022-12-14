// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class NamespacePrefixAnalyzerTest1 : DiagnosticVerifier
	{

		#region Non-Public Data Members

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

		#endregion

		#region Non-Public Properties/Methods
		private DiagnosticResultLocation GetBaseDiagnosticLocation(int rowOffset = 0, int columnOffset = 0)
		{
			return new DiagnosticResultLocation("Test.cs", 4 + rowOffset, 14 + columnOffset);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new NamespacePrefixAnalyzer();
		}

		#endregion


		#region Test Methods

		[TestMethod]
		public void ReportEmptyNamespacePrefix()
		{

			string code = string.Format(ClassString, "");
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.NamespacePrefix),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation(0,0)
				}
			};

			VerifyCSharpDiagnostic(code, expected);
		}

		#endregion
	}
}
