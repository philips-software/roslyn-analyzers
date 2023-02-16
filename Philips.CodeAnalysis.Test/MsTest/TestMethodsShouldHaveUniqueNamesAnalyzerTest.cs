﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestMethodsShouldHaveUniqueNamesAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestMethodsShouldHaveUniqueNamesAnalyzer();
		}

		#endregion

		#region Public Interface

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MethodsMustHaveUniqueNamesTestAsync()
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[TestMethod]
	public void Foo() { }

	[DataRow(null)]
	[DataTestMethod]
	public void Foo(object o) { }

	[DataRow(null, null)]
	[DataTestMethod]
	public void Foo(object o, object y) { }
}";

			await VerifyDiagnostic(code, new[]{
				new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.TestMethodsMustHaveUniqueNames),
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, null) },
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				},
				new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.TestMethodsMustHaveUniqueNames),
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, null) },
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				}
			}).ConfigureAwait(false);
		}

		#endregion
	}
}
