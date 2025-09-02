// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class TestTimeoutsClassAccessibilityAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestTimeoutsClassAccessibilityAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new TestTimeoutsClassAccessibilityCodeFixProvider();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestTimeoutsClassPublicShouldTriggerDiagnostic()
		{
			const string code = @"public class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			const string expectedCode = @"internal class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			await VerifyDiagnostic(code, DiagnosticId.TestTimeoutsClassShouldBeInternal).ConfigureAwait(false);
			await VerifyFix(code, expectedCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestTimeoutsClassInternalShouldNotTriggerDiagnostic()
		{
			const string code = @"internal class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestTimeoutsClassPublicSealedShouldTriggerDiagnostic()
		{
			const string code = @"public sealed class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			const string expectedCode = @"internal sealed class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			await VerifyDiagnostic(code, DiagnosticId.TestTimeoutsClassShouldBeInternal).ConfigureAwait(false);
			await VerifyFix(code, expectedCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestTimeoutsClassInternalStaticShouldTriggerDiagnostic()
		{
			const string code = @"internal static class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			const string expectedCode = @"internal class TestTimeouts
{
	public const int CiAppropriate = 1000;
}";

			await VerifyDiagnostic(code, DiagnosticId.TestTimeoutsClassShouldBeInternal).ConfigureAwait(false);
			await VerifyFix(code, expectedCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task OtherClassNameShouldNotTriggerDiagnostic()
		{
			const string code = @"public class SomeOtherClass
{
	public const int CiAppropriate = 1000;
}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}
	}
}
