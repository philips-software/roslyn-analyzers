// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class TestMethodsMustBeInTestClassAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TestMethodsMustBeInTestClassAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataRow("[TestMethod]")]
		[DataRow("[DataTestMethod]")]
		[DataRow("[AssemblyInitialize]")]
		[DataRow("[AssemblyCleanup]")]
		[DataRow("[ClassInitialize]")]
		[DataRow("[ClassCleanup]")]
		[DataTestMethod]
		public void TestMethodsMustBeInTestClass(string testType)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

{0}
public class Tests
{{
	{1}
	public void Foo() {{ }}
}}";

			VerifyCSharpDiagnostic(string.Format(code, "[TestClass]", testType));


			VerifyCSharpDiagnostic(string.Format(code, "", testType), new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustBeInTestClass),
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, null) },
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
			});
		}

		#endregion
	}
}
