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
	public class NoRegionsInMethodAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NoRegionsInMethodAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoRegionNoMethodTestAsync()
		{
			await VerifySuccessfulCompilation(@"class C{C(){}}").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoRegionTestAsync()
		{
			await VerifySuccessfulCompilation(@"class C{C(){}public void foo(){}}").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyClassWithRegionTestAsync()
		{
			await VerifySuccessfulCompilation(@"class C{	#region testRegion	#endregion	}").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionOutsideMethodTestAsync()
		{
			await VerifySuccessfulCompilation(@"class C{#region testRegion	public void foo() {int x = 2; }	#endregion}").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionStartsAndEndsInMethodTestAsync()
		{
			await VerifyDiagnostic(@"
class C
{
	public void foo()
	{
#region testRegion
		int x = 2;
#endregion
	}
}", 2).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionStartsInMethodTestAsync()
		{
			await VerifyDiagnostic(@"class C{ public void foo(){ #region testRegion int x = 2;}	#endregion

	}").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionEndsInMethodTestAsync()
		{
			await VerifyDiagnostic(@"class C{
	#region testRegion
	public void foo(){
		int x = 2;
		#endregion
	}
	}").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionCoversMultipleMethodsTestAsync()
		{
			await VerifySuccessfulCompilation(@"class C{	#region testRegion	public void foo(){	return;	} public void bar(){	}	#endregion	}").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionBeforeClassTestAsync()
		{
			await VerifySuccessfulCompilation(@"	#region testRegion	#endregion class C{	public void foo(){	return; }	public void bar(){	}	}").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UnnamedRegionTestAsync()
		{
			await VerifySuccessfulCompilation(@"class C{	#region #endregion	public void foo(){	return; }public void bar(){}	}").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionStartsInOneMethodEndsInAnotherTestAsync()
		{
			await VerifyDiagnostic(@"
class C{
	#region 
	public void foo(){
		return;
	}
	public void bar(){
			#endregion

	}

	}").ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MalformedCodeTestAsync()
		{
			await VerifySuccessfulCompilation(@"class C{	#region 	public void foo(){		return;	}	#endregion	public void bar(){	}	").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyStringTestAsync()
		{
			await VerifySuccessfulCompilation("").ConfigureAwait(false);
		}
	}
}
