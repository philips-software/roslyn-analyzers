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

		private const string CorrectSingle = @"
using System.Collections.Generic;
public static class Foo
{{
  public static void Method(List<int> data)
  {{
    data.Where(i => i == 0);
  }}
}}
";

		private const string CorrectMultiple = @"
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

		private const string CorrectParenthesized = @"
using System.Collections.Generic;
public static class Foo
{{
  public static void Method(List<int> data)
  {{
    data
      .Where((i) => i == 0)
      .Select((d) => { return d.ToString();});
  }}
}}
";

		private const string WrongMultiple = $@"
using System.Collections.Generic;
public static class Foo
{{
  public static void Method(List<int> data)
  {{
    data.Where(i => i == 0).Select(d => d.ToString());
  }}
}}
";

		private const string WrongParenthesized = $@"
using System.Collections.Generic;
public static class Foo
{{
  public static void Method(List<int> data)
  {{
    data.Where((i) => i == 0).Select((d) => {{ return d.ToString();}});
  }}
}}
";

		[DataTestMethod]
		[DataRow(WrongMultiple, DisplayName = nameof(WrongMultiple)),
		 DataRow(WrongParenthesized, DisplayName = nameof(WrongParenthesized))]
		public void FlagWhen2LambdasOnSameLine(string input)
		{

			VerifyCSharpDiagnostic(input, DiagnosticResultHelper.Create(DiagnosticIds.AvoidMultipleLambdasOnSingleLine));
		}


		[DataTestMethod]
		[DataRow(CorrectSingle, DisplayName = nameof(CorrectSingle)),
		 DataRow(CorrectMultiple, DisplayName = nameof(CorrectMultiple)),
		 DataRow(CorrectParenthesized, DisplayName = nameof(CorrectParenthesized))]
		public void CorrectDoesNotFlag(string input)
		{
			VerifyCSharpDiagnostic(input);
		}

		[TestMethod]
		public void GeneratedFileWrongIsNotFlagged()
		{
			VerifyCSharpDiagnostic(WrongMultiple, @"Foo.designer");
		}
	}
}
