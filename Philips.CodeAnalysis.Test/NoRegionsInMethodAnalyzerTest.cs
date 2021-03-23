using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class NoRegionsInMethodAnalyzerTest : DiagnosticVerifier
	{

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{

			return new NoRegionsInMethodAnalyzer();
		}

		[TestMethod]

		public void NoRegionNoMethodTest()
		{
			VerifyNoDiagnostic(@"Class C{C(){}}");
		}

		[TestMethod]
		public void NoRegionTest()
		{
			VerifyNoDiagnostic(@"Class C{C(){}public void foo(){}}");
		}

		[TestMethod]
		public void EmptyClassWithRegionTest()
		{
			VerifyNoDiagnostic(@"Class C{	#region testRegion	#endregion	}");
		}

		[TestMethod]
		public void RegionOutsideMethodTest()
		{
			VerifyNoDiagnostic(@"Class C{#region testRegion	public void foo() {int x = 2; }	#endregion}");
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
	}");
		}

		[TestMethod]
		public void RegionCoversMultipleMethodsTest()
		{
			VerifyNoDiagnostic(@"Class C{	#region testRegion	public void foo(){	return;	} public void bar(){	}	#endregion	}");
		}

		[TestMethod]
		public void RegionBeforeClassTest()
		{
			VerifyNoDiagnostic(@"	#region testRegion	#endregion Class C{	public void foo(){	return; }	public void bar(){	}	}");
		}

		[TestMethod]
		public void UnnamedRegionTest()
		{
			VerifyNoDiagnostic(@"Class C{	#region #endregion	public void foo(){	return; }public void bar(){}	}");
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

	}", 7);
		}


		[TestMethod]
		public void MalformedCodeTest()
		{
			VerifyNoDiagnostic(@"Class C{	#region 	public void foo(){		return;	}	#endregion	public void bar(){	}	");
		}

		[TestMethod]
		public void EmptyStringTest()
		{
			VerifyNoDiagnostic("");
		}




		//***********Methods under this line were taken from AvoidInLineNewAnalyzerTest.cs and modified

		private void VerifyNoDiagnostic(string file)
		{
			VerifyCSharpDiagnostic(file, new DiagnosticResult[0]);
		}
		private void VerifyDiagnostic(string file)
		{
			VerifyDiagnostic(file, 3);
		}

		private void VerifyDiagnostic(string file, int line)
		{
			VerifyCSharpDiagnostic(file, new DiagnosticResult()
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
