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

		[DataRow(true, null, "-1", true)]
		[DataRow(true, null, "1", true)]
		[DataRow(true, null, "0", true)]
		[DataRow(true, null, "-1u", true)]
		[DataRow(true, null, "1u", true)]
		[DataRow(true, null, "0u", true)]
		[DataRow(true, "-1", null, false)]
		[DataRow(true, "1", null, false)]
		[DataRow(true, "0", null, false)]
		[DataRow(true, "-1u", null, false)]
		[DataRow(true, "1u", null, false)]
		[DataRow(true, "0u", null, false)]
		[DataRow(false, null, "-1", true)]
		[DataRow(false, null, "1", true)]
		[DataRow(false, null, "0", true)]
		[DataRow(false, null, "-1u", true)]
		[DataRow(false, null, "1u", true)]
		[DataRow(false, null, "0u", true)]
		[DataRow(false, "-1", null, false)]
		[DataRow(false, "1", null, false)]
		[DataRow(false, "0", null, false)]
		[DataRow(false, "-1u", null, false)]
		[DataRow(false, "1u", null, false)]
		[DataRow(false, "0u", null, false)]
		[DataTestMethod]
		public void CheckNegativeInteger(bool shouldWrapArgument, string arg0, string arg1, bool isError)
		{
			string expectedParameter = arg0 ?? "GetValue()";
			string actualParameter = arg1 ?? "GetValue()";

			if (shouldWrapArgument)
			{
				if (arg0 is null)
				{
					expectedParameter = $"({expectedParameter})";
				}

				if (arg1 is null)
				{
					actualParameter = $"({actualParameter})";
				}
			}

			string template = @$"
int GetValue()
{{
	return 0;
}}

Assert.AreEqual({expectedParameter}, {actualParameter});
";

			if (isError)
			{
				VerifyError(template);
			}
			else
			{
				VerifyNoError(template);
			}
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
