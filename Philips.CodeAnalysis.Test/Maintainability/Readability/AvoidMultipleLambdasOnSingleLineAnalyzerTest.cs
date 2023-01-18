﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class AvoidMultipleLambdasOnSingleLineAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidMultipleLambdasOnSingleLineAnalyzer();
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new AvoidMultipleLambdasOnSingleLineCodeFixProvider();
		}

		private const string CorrectNoLambda = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static int Method(List<int> data)
  {
    return data.[0];
  }
}
";

		private const string CorrectSingle = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method(List<int> data)
  {
    data.Where(i => i == 0);
  }
}
";

		private const string CorrectMultiple = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method(List<int> data)
  {
    data.Where(i => i == 0)
      .Select(d => d.ToString());
  }
}
";

		private const string CorrectMoreLines = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method(List<int> data)
  {
    data
      .Where(i => i == 0)
      .Select(d => d.ToString());
  }
}
";

		private const string CorrectDistinct = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method(List<int> data)
  {
    data.Where(i => i == 0);
    data.Select(d => d.ToString());
  }
}
";

		private const string CorrectParenthesized = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method(List<int> data)
  {
    data.Where((i) => i == 0)
      .Select((d) => { return d.ToString(); });
  }
}
";

		private const string WrongMultiple = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method(List<int> data)
  {
    data.Where(i => i == 0).Select(d => d.ToString());
  }
}
";

		private const string WrongDistinct = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method(List<int> data)
  {
    data.Where(i => i == 0); data.Select(d => d.ToString());
  }
}
";

		private const string WrongParenthesized = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method(List<int> data)
  {
    data.Where((i) => i == 0).Select((d) => { return d.ToString();});
  }
}
";

		[DataTestMethod]
		[DataRow(WrongMultiple, CorrectMultiple, DisplayName = nameof(WrongMultiple)),
		 DataRow(WrongDistinct, CorrectDistinct, DisplayName = nameof(WrongDistinct)),
		 DataRow(WrongParenthesized, CorrectParenthesized, DisplayName = nameof(WrongParenthesized))]
		public void FlagWhen2LambdasOnSameLine(string input, string fixedCode)
		{

			VerifyCSharpDiagnostic(input, DiagnosticResultHelper.Create(DiagnosticIds.AvoidMultipleLambdasOnSingleLine));
			VerifyCSharpFix(input, fixedCode);
		}


		[DataTestMethod]
		[DataRow(CorrectNoLambda, DisplayName = nameof(CorrectNoLambda)), 
		 DataRow(CorrectSingle, DisplayName = nameof(CorrectSingle)),
		 DataRow(CorrectMultiple, DisplayName = nameof(CorrectMultiple)),
		 DataRow(CorrectDistinct, DisplayName = nameof(CorrectDistinct)),
		 DataRow(CorrectMoreLines, DisplayName = nameof(CorrectMoreLines)),
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
