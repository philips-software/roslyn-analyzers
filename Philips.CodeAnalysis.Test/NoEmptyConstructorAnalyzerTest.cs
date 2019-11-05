// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class NoEmptyConstructorAnalyzerTest : CodeFixVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		#endregion

		#region Public Interface

		[TestMethod]
		public void NoEmptyConstructorPartialDoesntCrash()
		{
			const string template = @"public class Foo 
{{
  static Foo()

}}
";
			string classContent = template;

			DiagnosticResult[] results = Array.Empty<DiagnosticResult>();

			VerifyCSharpDiagnostic(classContent, results);
		}

		[DataRow("static", "", true)]
		[DataRow("", "", false)]
		[DataRow("", "int x = 4;", false)]
		[DataRow("static", "int x = 4;", false)]
		[DataTestMethod]
		public void NoEmptyConstructorStatic(string modifier, string content, bool isError)
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

			DiagnosticResult[] results;
			if (isError)
			{
				results = new[] { new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticIds.RemoveEmptyConstructors),
						Message = new Regex(".*"),
						Severity = DiagnosticSeverity.Error,
						Locations = new[]
						{
							new DiagnosticResultLocation("Test0.cs", 5, 3)
						}
					}
				};
			}
			else
			{
				results = Array.Empty<DiagnosticResult>();
			}

			VerifyCSharpDiagnostic(classContent, results);
		}

		[DataRow("  /// <summary />")]
		[DataRow(@"  /** <summary>
  </summary> */")]
		[DataTestMethod]
		public void NoEmptyConstructorStaticWithFix(string summaryComment)
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

			VerifyCSharpFix(classContent, expected);
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new NoEmptyConstructorCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new NoEmptyConstructorAnalyzer();
		}

		#endregion
	}
}
