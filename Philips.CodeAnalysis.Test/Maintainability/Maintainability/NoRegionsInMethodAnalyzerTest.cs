using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

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

		public void NoRegionNoMethodTest()
		{
			VerifySuccessfulCompilation(@"Class C{C(){}}");
		}

		[TestMethod]
		public void NoRegionTest()
		{
			VerifySuccessfulCompilation(@"Class C{C(){}public void foo(){}}");
		}

		[TestMethod]
		public void EmptyClassWithRegionTest()
		{
			VerifySuccessfulCompilation(@"Class C{	#region testRegion	#endregion	}");
		}

		[TestMethod]
		public void RegionOutsideMethodTest()
		{
			VerifySuccessfulCompilation(@"Class C{#region testRegion	public void foo() {int x = 2; }	#endregion}");
		}

		[TestMethod]
		public void RegionStartsAndEndsInMethodTest()
		{
			VerifyDiagnostic(@"Class C{	public void foo(){#region testRegion int x = 2;	#endregion }}", 2);
		}

		[TestMethod]
		public void RegionStartsInMethodTest()
		{
			VerifyDiagnostic(@"Class C{ public void foo(){ #region testRegion int x = 2;}	#endregion

	}", 2);
		}

		[TestMethod]
		public void RegionEndsInMethodTest()
		{
			VerifyDiagnostic(@"Class C{
	#region testRegion
	public void foo(){
		int x = 2;
		#endregion
	}
	}", 5);
		}

		[TestMethod]
		public void RegionCoversMultipleMethodsTest()
		{
			VerifySuccessfulCompilation(@"Class C{	#region testRegion	public void foo(){	return;	} public void bar(){	}	#endregion	}");
		}

		[TestMethod]
		public void RegionBeforeClassTest()
		{
			VerifySuccessfulCompilation(@"	#region testRegion	#endregion Class C{	public void foo(){	return; }	public void bar(){	}	}");
		}

		[TestMethod]
		public void UnnamedRegionTest()
		{
			VerifySuccessfulCompilation(@"Class C{	#region #endregion	public void foo(){	return; }public void bar(){}	}");
		}

		[TestMethod]
		public void RegionStartsInOneMethodEndsInAnotherTest()
		{
			VerifyDiagnostic(@"
Class C{
	#region 
	public void foo(){
		return;
	}
	public void bar(){
			#endregion

	}

	}", 8);
		}


		[TestMethod]
		public void MalformedCodeTest()
		{
			VerifySuccessfulCompilation(@"Class C{	#region 	public void foo(){		return;	}	#endregion	public void bar(){	}	");
		}

		[TestMethod]
		public void EmptyStringTest()
		{
			VerifySuccessfulCompilation("");
		}




		private void VerifyDiagnostic(string file, int line)
		{
			VerifyDiagnostic(file, new DiagnosticResult()
			{
				Id = NoRegionsInMethodAnalyzer.Rule.Id,
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", line, -1), //6,-1
				}
			});
		}
	}
}
