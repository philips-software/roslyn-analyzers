// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class MergeIfStatementsAnalyzerTest : CodeFixVerifier
	{
		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new MergeIfStatementsCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new MergeIfStatementsAnalyzer();
		}


		[DataTestMethod]
		[DataRow(@"if (1==1) { if(1==1) {} else {} }")]			// Has else clause
		[DataRow(@"if (1==1) { int x; if (1==1) {} }")]         // Has multiple statements
		[DataRow(@"if (1==1) { if (1==1) {} ; int x}")]         // Has multiple statements
		[DataRow(@"{ if (1==1) {} }")]							// Parent is not if statement
		[DataRow(@"if (1==1) { if (1==1) {} } else {}")]        // Parent has else clause      
		[DataRow(@"if (1==1) { if (1==1 || 2==2) {} }")]        // Has ||      
		[DataRow(@"if (1==1 || 2==2) { if (1==1) {} }")]        // Parent has ||      
		[DataRow(@"if (1==1 || 2==2) if (2==2) {}")]			// Parent has ||, no { }
		public void DoNotMergeIfsTest(string test)
		{
			const string testCodeTemplate = @"
		        public class MyClass
				{{
					public void Foo()
					{{
						{0}
					}}
			    }}";

			string testCode = string.Format(testCodeTemplate, test);
			VerifyCSharpDiagnostic(testCode);
		}

		[DataTestMethod]
		[DataRow(@"if(1==1) { if (2==2) {} }", @"if (1 == 1 && 2 == 2)")]
		[DataRow(@"if (3==3) if (4==4) {}", @"if (3 == 3 && 4 == 4)")]
		public void MergeIfsTest(string test, string fixedTest)
		{
			fixedTest += Environment.NewLine + "{ }";
			const string testCodeTemplate = @"
		        public class MyClass
				{{
					public void Foo()
					{{
						// Comment
						{0}
					}}
			    }}";


			string testCode = string.Format(testCodeTemplate, test);
			string fixedCode = string.Format(testCodeTemplate, fixedTest);

			var expectedDiagnostic = DiagnosticResultHelper.Create(DiagnosticIds.MergeIfStatements);
			VerifyCSharpDiagnostic(testCode, expectedDiagnostic);
			VerifyCSharpFix(testCode, fixedCode);
		}
	}
}
