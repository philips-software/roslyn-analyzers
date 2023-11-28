// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidCastToStringAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidCastToStringAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidExplicitCastToString()
		{
			var code = @"
class A
{
  public static explicit operator string(A a)
  {
    return string.Empty;
  }
}
";
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidImplicitCastToString()
		{
			var code = @"
class A
{
  public static implicit operator string(A a)
  {
    return string.Empty;
  }
}
";
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CastToOtherTypeIsAllowed()
		{
			var code = @"
class A
{
  public static implicit operator int(A a)
  {
    return 42;
  }
}
";
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}
	}
}
