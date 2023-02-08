// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class AvoidMultipleLambdasOnSingleLineAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidMultipleLambdasOnSingleLineAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
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
    data.Where((i) => i == 0).Select((d) => { return d.ToString(); });
  }
}
";

		[DataTestMethod]
		[DataRow(WrongMultiple, CorrectMultiple, DisplayName = nameof(WrongMultiple)),
		 DataRow(WrongDistinct, CorrectDistinct, DisplayName = nameof(WrongDistinct)),
		 DataRow(WrongParenthesized, CorrectParenthesized, DisplayName = nameof(WrongParenthesized))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagWhen2LambdasOnSameLine(string input, string fixedCode)
		{

			await VerifyDiagnostic(input, DiagnosticId.AvoidMultipleLambdasOnSingleLine).ConfigureAwait(false);
			await VerifyFix(input, fixedCode).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow(CorrectNoLambda, DisplayName = nameof(CorrectNoLambda)), 
		 DataRow(CorrectSingle, DisplayName = nameof(CorrectSingle)),
		 DataRow(CorrectMultiple, DisplayName = nameof(CorrectMultiple)),
		 DataRow(CorrectDistinct, DisplayName = nameof(CorrectDistinct)),
		 DataRow(CorrectMoreLines, DisplayName = nameof(CorrectMoreLines)),
		 DataRow(CorrectParenthesized, DisplayName = nameof(CorrectParenthesized))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CorrectDoesNotFlagAsync(string input)
		{
			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedFileWrongIsNotFlaggedAsync()
		{
			await VerifySuccessfulCompilation(WrongMultiple, @"Foo.designer").ConfigureAwait(false);
		}
	}
}
