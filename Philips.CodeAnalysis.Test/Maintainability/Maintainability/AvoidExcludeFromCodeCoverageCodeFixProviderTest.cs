// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidExcludeFromCodeCoverageCodeFixProviderTest : CodeFixVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RemoveExcludeFromCodeCoverageAttributeOnMethod()
		{
			var given = @"
using System.Diagnostics.CodeAnalysis;
class TestClass 
{
  [ExcludeFromCodeCoverage]
  public void TestMethod()
  {
    return;
  }
}
";

			var expected = @"
using System.Diagnostics.CodeAnalysis;
class TestClass 
{
  
  public void TestMethod()
  {
    return;
  }
}
";

			await VerifyFix(given, expected, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RemoveExcludeFromCodeCoverageAttributeOnClass()
		{
			var given = @"
using System.Diagnostics.CodeAnalysis;
[ExcludeFromCodeCoverage]
class TestClass 
{
}
";

			var expected = @"
using System.Diagnostics.CodeAnalysis;

class TestClass 
{
}
";

			await VerifyFix(given, expected, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RemoveExcludeFromCodeCoverageAttributeButKeepOthers()
		{
			var given = @"
using System;
using System.Diagnostics.CodeAnalysis;
class TestClass 
{
  [Obsolete, ExcludeFromCodeCoverage]
  public void TestMethod()
  {
    return;
  }
}
";

			var expected = @"
using System;
using System.Diagnostics.CodeAnalysis;
class TestClass 
{
  [Obsolete]
  public void TestMethod()
  {
    return;
  }
}
";

			await VerifyFix(given, expected, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RemoveExcludeFromCodeCoverageWithAlias()
		{
			var given = @"
using System.Diagnostics.CodeAnalysis;
using CodeCoverageAlias = System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage;
class TestClass 
{
  [CodeCoverageAlias]
  public void TestMethod()
  {
    return;
  }
}
";

			var expected = @"
using System.Diagnostics.CodeAnalysis;
using CodeCoverageAlias = System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage;
class TestClass 
{
  
  public void TestMethod()
  {
    return;
  }
}
";

			await VerifyFix(given, expected, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidExcludeFromCodeCoverageCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidExcludeFromCodeCoverageAnalyzer();
		}
	}
}
