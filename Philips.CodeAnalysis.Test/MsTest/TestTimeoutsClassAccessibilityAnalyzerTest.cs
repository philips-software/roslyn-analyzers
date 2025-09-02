// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
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
	public class TestTimeoutsClassAccessibilityAnalyzerTest : AssertCodeFixVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestTimeoutsClassPublic_ShouldTriggerDiagnostic()
		{
			const string code = @"
public class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			const string expectedCode = @"
internal class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			await VerifyDiagnosticAndCodeFix(code, expectedCode, GetExpectedDiagnostic(1, 14));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestTimeoutsClassPublicSealed_ShouldTriggerDiagnostic()
		{
			const string code = @"
public sealed class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			const string expectedCode = @"
internal sealed class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			await VerifyDiagnosticAndCodeFix(code, expectedCode, GetExpectedDiagnostic(1, 21));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestTimeoutsClassInternalStatic_ShouldTriggerDiagnostic()
		{
			const string code = @"
internal static class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			const string expectedCode = @"
internal class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			await VerifyDiagnosticAndCodeFix(code, expectedCode, GetExpectedDiagnostic(1, 23));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestTimeoutsClassInternal_ShouldNotTriggerDiagnostic()
		{
			const string code = @"
internal class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			await VerifyNoTrigger(code);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task OtherClassName_ShouldNotTriggerDiagnostic()
		{
			const string code = @"
public class SomeOtherClass
{
	public const int CiAppropriate = 1000;
}";

			await VerifyNoTrigger(code);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestTimeoutsClassNoModifiers_ShouldNotTriggerDiagnostic()
		{
			const string code = @"
class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			await VerifyNoTrigger(code);
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.TestTimeoutsClassShouldBeInternal),
				Message = "Class 'TestTimeouts' should be declared as internal",
				Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", expectedLineNumberErrorOffset + 1, expectedColumnErrorOffset + 1)
				}
			};
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new TestTimeoutsClassAccessibilityCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestTimeoutsClassAccessibilityAnalyzer();
		}
	}
}