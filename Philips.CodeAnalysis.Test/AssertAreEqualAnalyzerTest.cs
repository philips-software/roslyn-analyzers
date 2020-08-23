// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class AssertAreEquaAnalyzerTest : AssertCodeFixVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AssertAreEqual),
				Location = new DiagnosticResultLocation("Test0.cs", null, null),
				Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
			};
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AssertAreEqualAnalyzer();
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new AssertAreEqualCodeFixProvider();
		}

		#endregion

		#region Public Interface

		[TestMethod]
		public void CheckDefaultBehavior()
		{
			VerifyNoError(@"
string GetValue()
{
	return string.Empty;
}

Assert.AreEqual(default, GetValue());
");
		}

		[TestMethod]
		public void CheckWillIgnoreTypeArgument()
		{
			VerifyError(@"
string GetValue()
{
	return string.Empty;
}

Assert.AreEqual<string>(GetValue(), null);
");
		}

		#endregion
	}
}
