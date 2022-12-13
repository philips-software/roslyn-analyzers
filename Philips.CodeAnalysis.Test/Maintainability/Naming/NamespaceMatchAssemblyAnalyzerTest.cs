using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class NamespaceMatchAssemblyAnalyzerTest : DiagnosticVerifier
	{

		#region Non-Public Data Members

		private const string ClassString = @"
			using System;
			using System.Globalization;
			namespace {0}
			{{
			class Foo 
			{{
				public void Foo()
				{{
				}}
			}}
			}}
			";

		private const string configuredPrefix = @"Philips.";

		#endregion

		#region Non-Public Properties/Methods
		private DiagnosticResultLocation GetBaseDiagnosticLocation(string path, int rowOffset = 0, int columnOffset = 0)
		{
			return new DiagnosticResultLocation(path + ".cs", 4 + rowOffset, 14 + columnOffset);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new NamespaceMatchAssemblyAnalyzer();
		}

		#endregion


		#region Test Methods
		[DataTestMethod]
		[DataRow("Philips.Test", "C:\\development\\Philips.Production\\code\\MyTest")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Production\\MyAnalyzer")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.TestFramework\\MyHelper")]
		public void ReportIncorrectNamespaceMatch(string prefix, string path)
		{

			string code = string.Format(ClassString, prefix);
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.NamespaceMatchAssembly),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation(path, 0,0)
				}
			};

			VerifyCSharpDiagnostic(code, path, expected);
		}

		[DataTestMethod]
		[DataRow("Philips.Test", "C:\\development\\Philips.Test\\code\\MyTest")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Test\\MyTest")]
		public void DoNotReportANamespaceMatchError(string ns, string path)
		{
			string code = string.Format(ClassString, ns);
			VerifyCSharpDiagnostic(code, path, Array.Empty<DiagnosticResult>());
		}

		#endregion
	}
}
