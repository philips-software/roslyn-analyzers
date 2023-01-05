// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class AvoidMultipleLambdsaOnSingleLineAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidMultipleLambdasOnSingleLineAnalyzer();
		}

		private const string Correct = @"
using System.Collections.Generic;
public static class Foo
{{
  public static void Method(List<int> data)
  {{
    data
      .Where(i => i == 0)
      .Select(d => d.ToString());
  }}
}}
";

		private const string Wrong = $@"
using System.Collections.Generic;
public static class Foo
{{
  public static void Method(List<int> data)
  {{
    data.Where(i => i == 0).Select(d => d.ToString());
  }}
}}
";

		[TestMethod]
		public void FlagWhen2LambdasOnSameLine()
		{

			VerifyCSharpDiagnostic(Wrong, DiagnosticResultHelper.Create(DiagnosticIds.AvoidMultipleLambdasOnSingleLine));
		}


		[TestMethod]
		public void CorrectDoesNotFlag()
		{
			VerifyCSharpDiagnostic(Correct);
		}


		[TestMethod]
		public void GeneratedFileWrongIsNotFlagged()
		{
			VerifyCSharpDiagnostic(Wrong, @"Foo.designer");
		}
	}
}
