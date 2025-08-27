// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class AvoidEmptyRegionsAnalyzerTest : CodeFixVerifier
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

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionContainingNestedClassWorks()
		{
			// This should work (and does work) - region in outer class containing nested class
			var baseline = @"
class Foo
{{
	#region
	class Cat
	{{
		private int meow;
	}}
	#endregion
}}";
			await VerifySuccessfulCompilation(baseline).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SimpleNestedClassWithRegion()
		{
			// Let's try a simpler case - nested class with region containing just a method
			var baseline = @"
class Foo
{{
	class Cat
	{{
		#region
		private void Meow() {{ }}
		#endregion
	}}
}}";
			await VerifySuccessfulCompilation(baseline).ConfigureAwait(false);

		}
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidEmptyRegion()
		{
			var givenText = @"
class C {{
	#region Dictionaries
	#endregion
}}
";
			await VerifyDiagnostic(givenText, DiagnosticId.AvoidEmptyRegions).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NestedRegionEmptyInnerRegionTriggersDiagnostic()
		{
			var givenText = @"
public class MyClass
{
	#region OuterRegion
	private void Foo1() { }
	
	#region InnerEmptyRegion
	#endregion
	
	private void Foo2() { }
	#endregion
}
";
			await VerifyDiagnostic(givenText, DiagnosticId.AvoidEmptyRegions).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidEmptyRegionFalsePositive1()
		{
			// 2 Analyses/sets triggered, but first #endregion is with second set (which now has 3 items)
			var givenText = @"
namespace MyNamespace {{
	#region Dictionaries
	public class StringToActionDictionary {{ }}
	#endregion

	#region Lists
	public class ObjectList {{ }}
	#endregion
}}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidEmptyRegionFalsePositive2()
		{
			// 2 Analyses/sets triggered, but first #endregion is with second set (which should have 3 items (not good), but
			// last #endregion is excluded, so perceived as a pair, starting with an #endregion.
			var givenText = @"
	#region Dictionaries
	public class StringToActionDictionary {{ }}
	#endregion

	#region Lists
	public class ObjectList {{ }}
	#endregion
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionWithCopyrightShouldNotTriggerDiagnostic()
		{
			const string input = @"public class Foo
{
  #region Header
  // © 2019 Koninklijke Philips N.V.  All rights reserved.
  // Reproduction or transmission in whole or in part, in any form or by any means, 
  // electronic, mechanical or otherwise, is prohibited without the prior  written consent of 
  // the owner.
  #endregion
}";
			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new EnforceRegionsRemoveEmptyRegionCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidEmptyRegionsAnalyzer();
		}
	}
}
