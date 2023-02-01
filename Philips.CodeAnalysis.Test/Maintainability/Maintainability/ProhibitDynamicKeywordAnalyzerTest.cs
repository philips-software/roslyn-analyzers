// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class ProhibitDynamicKeywordAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new ProhibitDynamicKeywordAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataRow(@"void TestMethod() { dynamic i = 5; }", 1)]
		[DataRow(@"dynamic TestMethod() { return 5; }", 1)]
		[DataRow(@"void TestMethod(dynamic i) { return 5; }", 1)]
		[DataRow(@"void TestMethod() { List<dynamic> list = null; }", 1)]
		[DataRow(@"void TestMethod() { var t = (dynamic)4; }", 2)]
		[DataRow(@"void TestMethod() { string dynamic = ""test""; }", 0)]
		[DataRow(@"void TestMethod() { string dynamic = mrModule.DynamicSeries;
bool isDynamic = !String.IsNullOrEmpty(dynamic) &&
dynamic.StartsWith(""Y"", true, CultureInfo.CurrentCulture);
 }", 0)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CantBeDynamic(string testCode, int errorCount)
		{
			List<DiagnosticResult> results = new();
			for (int i = 0; i < errorCount; i++)
			{
				results.Add(DiagnosticResultHelper.Create(DiagnosticIds.DynamicKeywordProhibited));
			}

			var expected = results.ToArray();
			VerifyDiagnostic(testCode, expected);
		}

		#endregion
	}
}
