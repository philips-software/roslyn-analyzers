﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class NamespacePrefixAnalyzerTest : DiagnosticVerifier
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

		private const string configuredPrefix = @"Philips.iX";

		#endregion

		#region Non-Public Properties/Methods
		private DiagnosticResultLocation GetBaseDiagnosticLocation(int rowOffset = 0, int columnOffset = 0)
		{
			return new DiagnosticResultLocation("Test.cs", 4 + rowOffset, 14 + columnOffset);
		}


		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ $@"dotnet_code_quality.{ NamespacePrefixAnalyzer.RuleForIncorrectNamespace.Id }.namespace_prefix", configuredPrefix  }
			};
			return options;
		}
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new NamespacePrefixAnalyzer();
		}

		#endregion


		#region Test Methods
		[DataTestMethod]
		[DataRow("")]
		[DataRow("test")]
		[DataRow("Philips.Test")]
		public void ReportIncorrectNamespacePrefix(string prefix)
		{

			string code = string.Format(ClassString, prefix);
			DiagnosticResult expected = new DiagnosticResult
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

		[TestMethod]
		public void DoNotReportANamespacePrefixError()
		{
			string code = string.Format(ClassString, configuredPrefix + ".");
			VerifyCSharpDiagnostic(code, new DiagnosticResult[0]);
		}

		#endregion
	}
}
