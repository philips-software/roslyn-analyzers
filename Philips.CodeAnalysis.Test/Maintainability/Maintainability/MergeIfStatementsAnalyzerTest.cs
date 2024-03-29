﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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
	public class MergeIfStatementsAnalyzerTest : CodeFixVerifier
	{
		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new MergeIfStatementsCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new MergeIfStatementsAnalyzer();
		}


		[DataTestMethod]
		[DataRow(@"if (1==1) { if(1==1) {} else {} }", DisplayName = "Has else clause")]
		[DataRow(@"if (1==1) { int x; if (1==1) {} }", DisplayName = "Has multiple statements")]
		[DataRow(@"if (1==1) { if (1==1) {} ; int x}", DisplayName = "Has multiple statements")]
		[DataRow(@"{ if (1==1) {} }", DisplayName = "Parent is not if statement")]
		[DataRow(@"if (1==1) { if (1==1) {} } else {}", DisplayName = "Parent has else clause")]
		[DataRow(@"if (1==1) { if (1==1 || 2==2) {} }", DisplayName = "Has ||")]
		[DataRow(@"if (1==1 || 2==2) { if (1==1) {} }", DisplayName = "Parent has ||")]
		[DataRow(@"if (1==1 || 2==2) if (2==2) {}", DisplayName = "Parent has ||, no { }")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoNotMergeIfsTestAsync(string test)
		{
			const string testCodeTemplate = @"
		        public class MyClass
				{{
					public void Foo()
					{{
						{0}
					}}
			    }}";

			var testCode = string.Format(testCodeTemplate, test);
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"if(1==1) { if (2==2) {} }", @"if (1 == 1 && 2 == 2)")]
		[DataRow(@"if (3==3) if (4==4) {}", @"if (3 == 3 && 4 == 4)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MergeIfsTest(string test, string fixedTest)
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


			var testCode = string.Format(testCodeTemplate, test);
			var fixedCode = string.Format(testCodeTemplate, fixedTest);

			await VerifyDiagnostic(testCode).ConfigureAwait(false);
			await VerifyFix(testCode, fixedCode).ConfigureAwait(false);
		}
	}
}
