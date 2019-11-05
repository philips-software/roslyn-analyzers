// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class TestHasDescriptionTest : AssertCodeFixVerifier
	{

		public TestHasDescriptionTest()
		{
			OtherClassSyntax = @"class TestDescriptions { public const string longDescription = ""stringlongerthantwentyfivecharacters"";  public const string shortDescription = ""shortString"";};";
		}

		[DataTestMethod]
		[DataRow("[TestMethod, Description(TestDescriptions.longDescription)]", "[TestMethod]")]
		[DataRow("[TestMethod, Description(\"asdfasdkfasdfkasd\")]", "[TestMethod]")]
		public void IncorrectDescriptionAttribute(string methodAttributes, string expectedMethodAttributes)
		{
			VerifyChange(string.Empty, string.Empty, methodAttributes, expectedMethodAttributes);
		}


		[DataTestMethod]
		[DataRow("[TestMethod][Description(TestDescriptions.shortDescription)]")]
		[DataRow("[TestMethod, Description(TestDescriptions.shortDescription)]")]
		public void CorrectDescriptionAttribute(string methodAttributes)
		{
			VerifyNoChange(methodBody: string.Empty, methodAttributes: methodAttributes);
		}

		[TestMethod]
		public void AttributesInMethodsDontCauseCrash()
		{
			const string body = @"
[TestMethod, Description(TestDescriptions.shortDescription)]
var foo = 4;
";

			VerifyNoChange(methodBody: body, methodAttributes: "[TestMethod]");
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidDescriptionAttribute),
				Message = new Regex(TestHasDescriptionAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 12 + expectedLineNumberErrorOffset, 18 + expectedColumnErrorOffset)
				}
			};
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new TestHasDescriptionCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TestHasDescriptionAnalyzer();
		}
	}
}