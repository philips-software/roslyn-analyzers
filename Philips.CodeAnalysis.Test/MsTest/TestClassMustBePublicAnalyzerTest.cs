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
	public class TestClassMustBePublicAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestClassMustBePublicAnalyzer();
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
		public void TestClassMustBePublic(string modifier, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
{1} class Tests
{{
	{0}
	public void Foo() {{ }}
}}";

			foreach (string testType in new[] { "[TestMethod]", "[DataTestMethod]" })
			{
				string text = string.Format(code, testType, modifier);

				if (isCorrect)
				{
					VerifyDiagnostic(text);
				}
				else
				{
					VerifyDiagnostic(text, new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticIds.TestClassesMustBePublic),
						Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, modifier.Length + 8) },
						Message = new Regex(".*"),
						Severity = DiagnosticSeverity.Error,
					});
				}
			}
		}

		[DataRow("", false)]
		[DataRow("static", false)]
		[DataRow("public static", true)]
		[DataRow("private static", false)]
		[DataRow("private", false)]
		[DataRow("protected", false)]
		[DataRow("protected static", false)]
		[DataRow("public", false)]
		[DataTestMethod]
		public void TestClassWithAssemblyInitializeMustBePublicStatic(string modifier, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
{1} class Tests
{{
	{0}
	public static void Foo() {{ }}
}}";

			foreach (string testType in new[] { "[AssemblyInitialize]", "[AssemblyCleanup]" })
			{
				string text = string.Format(code, testType, modifier);

				if (isCorrect)
				{
					VerifyDiagnostic(text);
				}
				else
				{
					VerifyDiagnostic(text, new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticIds.TestClassesMustBePublic),
						Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, modifier.Length + 8) },
						Message = new Regex(".*"),
						Severity = DiagnosticSeverity.Error,
					});
				}
			}
		}

		#endregion
	}
}