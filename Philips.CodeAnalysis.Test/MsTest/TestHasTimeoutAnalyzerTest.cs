// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
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
	public class TestHasTimeoutAnalyzerTest : AssertCodeFixVerifier
	{
		public TestHasTimeoutAnalyzerTest()
		{
			OtherClassSyntax = @"
class TestTimeouts
{ 
	public const int CiAppropriate = 1;
	public const int Integration = 2;
	public const int CiAcceptable = 3;
	public const int Smoke = 4;
}

class TestDefinitions
{
	public const string UnitTests = ""Unit"";
	public const string IntegrationTests = ""Integration"";
	public const string SmokeTests = ""Smoke"";
}";
		}

		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			return base.GetAdditionalAnalyzerConfigOptions()
				.Add($"dotnet_code_quality.{TestHasTimeoutAnalyzer.Rule.Id}.Unit", "TestTimeouts.CiAppropriate,TestTimeouts.CiAcceptable")
				.Add($"dotnet_code_quality.{TestHasTimeoutAnalyzer.Rule.Id}.Integration", "TestTimeouts.Integration")
				.Add($"dotnet_code_quality.{TestHasTimeoutAnalyzer.Rule.Id}.Smoke", "TestTimeouts.Smoke");
		}

		[DataTestMethod]
		[DataRow("[STATestMethod]", "[STATestMethod]\n    [Timeout(1000)]")]
		[DataRow("[TestMethod]", "[TestMethod]\n    [Timeout(1000)]")]
		[DataRow("[TestMethod, Owner(\"\")]", "[TestMethod, Owner(\"\")]\n    [Timeout(1000)]")]
		[DataRow("[DataTestMethod]", "[DataTestMethod]\n    [Timeout(1000)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests)]", "[TestMethod, TestCategory(TestDefinitions.UnitTests)]\n    [Timeout(TestTimeouts.CiAppropriate)]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TimeoutAttributeNotPresent(string methodAttributes, string expectedMethodAttributes)
		{
			await VerifyChange(string.Empty, string.Empty, methodAttributes, expectedMethodAttributes).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("[STATestMethod]", "[STATestMethod]\n    [Timeout(1000)]")]
		[DataRow("[TestMethod]", "[TestMethod]\n    [Timeout(1000)]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TimeoutAttributeNotPresentNoCategory(string methodAttributes, string expectedMethodAttributes)
		{
			await VerifyChange(string.Empty, string.Empty, methodAttributes, expectedMethodAttributes).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("[STATestMethod, TestCategory(\"foo\")]", "[STATestMethod, TestCategory(\"foo\")]\n    [Timeout(1000)]")]
		[DataRow("[TestMethod, TestCategory(\"foo\")]", "[TestMethod, TestCategory(\"foo\")]\n    [Timeout(1000)]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TimeoutAttributeNotPresentUnknownCategory(string methodAttributes, string expectedMethodAttributes)
		{
			await VerifyChange(string.Empty, string.Empty, methodAttributes, expectedMethodAttributes).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("[STATestMethod, TestCategory(TestDefinitions.UnitTests), Timeout(TestTimeouts.Integration)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests), Timeout(TestTimeouts.Integration)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.IntegrationTests), Timeout(TestTimeouts.CiAppropriate)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.SmokeTests), Timeout(TestTimeouts.CiAppropriate)]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TimeoutAttributeWrongAsync(string methodAttributes)
		{
			await VerifyError(string.Empty, methodAttributes).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("[STATestMethod, TestCategory(TestDefinitions.UnitTests), Timeout(TestTimeouts.CiAppropriate)]")]
		[DataRow("[DataTestMethod, TestCategory(TestDefinitions.UnitTests), Timeout(TestTimeouts.CiAppropriate)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests), Timeout(TestTimeouts.CiAppropriate)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests), Timeout(TestTimeouts.CiAcceptable)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.IntegrationTests), Timeout(TestTimeouts.Integration)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.SmokeTests), Timeout(TestTimeouts.Smoke)]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TimeoutAttributeCorrect(string methodAttributes)
		{
			await VerifyNoChange(string.Empty, methodAttributes).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow("[TestMethod][Timeout(1)]")]
		[DataRow("[Timeout(1)][TestMethod]")]
		[DataRow("[TestMethod, Timeout(1)]")]
		[DataRow("[TestMethod, Owner(\"\"), Timeout(1)]")]
		[DataRow("[DataTestMethod, Timeout(1)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests)]\n [Timeout(TestTimeouts.CiAppropriate)]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TimeoutAttributePresent(string methodAttributes)
		{
			await VerifyNoChange(methodBody: string.Empty, methodAttributes: methodAttributes).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("[TestInitialize]")]
		[DataRow("[TestCleanup]")]
		[DataRow("[AssemblyInitialize]")]
		[DataRow("[DataRow]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotApplyToNonTestMethods(string methodAttributes)
		{
			await VerifyNoChange(methodBody: string.Empty, methodAttributes: methodAttributes).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AttributesInMethodsDontCauseCrash()
		{
			const string body = @"
[TestMethod, Timeout(1)]
var foo = 4;
";

			await VerifyNoChange(methodBody: body, methodAttributes: "[TestMethod, Timeout(1)]").ConfigureAwait(false);
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult
			{
				Id = DiagnosticId.TestHasTimeoutAttribute.ToId(),
				Message = new Regex(TestHasTimeoutAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", null, null)
				}
			};
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new TestHasTimeoutCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestHasTimeoutAnalyzer();
		}
	}
}
