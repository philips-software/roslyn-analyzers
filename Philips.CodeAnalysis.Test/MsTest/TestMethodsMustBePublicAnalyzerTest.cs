// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
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
	public class TestMethodsMustBePublicAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestMethodsMustBePublicAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataRow("", false)]
		[DataRow("static", false)]
		[DataRow("public static", false)]
		[DataRow("private static", false)]
		[DataRow("private", false)]
		[DataRow("protected", false)]
		[DataRow("public", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void TestMethodsMustBeInTestClass(string modifier, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	{0}
	{1} void Foo() {{ }}
}}";

			foreach (string testType in new[] { "[TestMethod]", "[DataTestMethod]" })
			{
				string text = string.Format(code, testType, modifier);

				if (isCorrect)
				{
					VerifySuccessfulCompilation(text);
				}
				else
				{
					VerifyDiagnostic(text, new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticId.TestMethodsMustBePublic),
						Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, modifier.Length + 8) },
						Message = new Regex(".*"),
						Severity = DiagnosticSeverity.Error,
					});
				}
			}
		}

		#endregion
	}
}
