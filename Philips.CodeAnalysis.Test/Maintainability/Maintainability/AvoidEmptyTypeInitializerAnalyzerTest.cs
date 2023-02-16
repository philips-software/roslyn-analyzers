﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidEmptyTypeInitializerAnalyzerTest : CodeFixVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidEmptyTypeInitializerPartialDoesNotCrashAsync()
		{
			const string template = @"public class Foo 
{{
  static Foo()

}}
";
			string classContent = template;
			await VerifySuccessfulCompilation(classContent).ConfigureAwait(false);
		}

		[DataRow("static", "", true)]
		[DataRow("", "", false)]
		[DataRow("", "int x = 4;", false)]
		[DataRow("static", "int x = 4;", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidEmptyTypeInitializerStaticAsync(string modifier, string content, bool isError)
		{
			const string template = @"public class Foo 
{{
  #region start
  /// <summary />
  {0} Foo() {{ {1} }}
  #endregion
}}
";
			string classContent = string.Format(template, modifier, content);

			if (isError)
			{
				await VerifyDiagnostic(classContent).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(classContent).ConfigureAwait(false);
			}
		}

		[DataRow("  /// <summary />")]
		[DataRow(@"  /** <summary>
  </summary> */")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidEmptyTypeInitializerStaticWithFix(string summaryComment)
		{
			const string template = @"public class Foo 
{{
  #region start
{0}
  #endregion
}}
";
			string classContent = string.Format(template, string.Format(@"{0}
static Foo() {{ }}", summaryComment));

			string expected = string.Format(template, "  \r\n");

			await VerifyFix(classContent, expected).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidEmptyTypeInitializerCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidEmptyTypeInitializerAnalyzer();
		}
	}
}
