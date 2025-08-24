// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class EnforceRegionsRemoveEmptyRegionAnalyzerTest : CodeFixVerifier
	{
		[DataRow("#region myRegion\n#endregion")]
		[DataRow("#region myRegion\r\n#endregion")]
		[DataRow("#region myRegion\r\n#endregion // My comment")]
		[DataRow("#region myRegion // My comment \r\n#endregion // My comment")]
		[DataRow("#region myRegion // My comment \r\n#endregion       ")]
		[DataRow("#region myRegion\r\n\r\n#endregion")]
		[DataRow("#region myRegion // comment\r\n#endregion")] // even with comment
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RemoveEmptyRegionTriggersDiagnosticAndFixes(string emptyRegionBlock)
		{
			var input = $@"public class Foo
{{
  {emptyRegionBlock}
}}";

			var expected = $@"public class Foo
{{
}}";

			await VerifyFix(input, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionWithExtraNewLinesAreReduced()
		{
			const string input = @"public class Foo
{

  #region myRegion
  #endregion

}";
			const string expected = @"public class Foo
{

}";
			await VerifyFix(input, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyRegionOnFirstLine()
		{
			const string input = @"#region Copyright
  #endregion
  public class Foo {}
}";
			const string expected = @"public class Foo {}
}";
			await VerifyFix(input, expected).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NestedRegionDoesNotTriggerEmptyRegion()
		{
			const string input = @"
	public class MyClass
	{
		#region R1
		private void Foo1() { }
		#endregion

		#region R2

		#region R3
		private void Foo2() { }
		#endregion

		#endregion
	}
";
			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}



		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionWithContentDoesNotTriggerDiagnostic()
		{
			const string input = @"public class Foo
{
  #region myRegion
  int x = 5;
  #endregion
}";
			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NestedClassesNonEmptyRegionTest()
		{
			var baseline = @"
class Foo
{{
	class Cat
	{{
		#region
		private int meow;
		#endregion
	}}
}}";
			await VerifySuccessfulCompilation(baseline).ConfigureAwait(false);

		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new EnforceRegionsRemoveEmptyRegionCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceRegionsAnalyzer();
		}
	}
}
