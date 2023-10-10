// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IncorrectDescriptionAttribute(string methodAttributes, string expectedMethodAttributes)
		{
			await VerifyChange(string.Empty, string.Empty, methodAttributes, expectedMethodAttributes).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow("[TestMethod][Description(TestDescriptions.shortDescription)]")]
		[DataRow("[TestMethod, Description(TestDescriptions.shortDescription)]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CorrectDescriptionAttribute(string methodAttributes)
		{
			await VerifyNoChange(methodBody: string.Empty, methodAttributes: methodAttributes).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AttributesInMethodsDontCauseCrash()
		{
			const string body = @"
[TestMethod, Description(TestDescriptions.shortDescription)]
var foo = 4;
";

			await VerifyNoChange(methodBody: body, methodAttributes: "[TestMethod]").ConfigureAwait(false);
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult
			{
				Id = DiagnosticId.AvoidDescriptionAttribute.ToId(),
				Message = new Regex(TestHasDescriptionAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 12 + expectedLineNumberErrorOffset, 18 + expectedColumnErrorOffset)
				}
			};
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new TestHasDescriptionCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestHasDescriptionAnalyzer();
		}
	}
}
