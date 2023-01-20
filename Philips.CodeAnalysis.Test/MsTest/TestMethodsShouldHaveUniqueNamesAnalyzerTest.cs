// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

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
		public void TestMethodsMustHaveUniqueNames()
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

			VerifyCSharpDiagnostic(code, new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustHaveUniqueNames),
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, null) },
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
			},
			new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustHaveUniqueNames),
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, null) },
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
			});
		}

		#endregion
	}
}