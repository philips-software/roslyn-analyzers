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

		private const string WrongWithIndentationIssue = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method<TKey>(Dictionary<TKey, object> dict, System.Func<object, bool> predicate)
  {
			List<TKey> items = dict.Where((kvp) => predicate(kvp.Value)).Select((kvp) => kvp.Key).ToList();
  }
}
";

		private const string CorrectWithProperIndentation = @"
using System.Collections.Generic;
using System.Linq;

public static class Foo
{
  public static void Method<TKey>(Dictionary<TKey, object> dict, System.Func<object, bool> predicate)
  {
			List<TKey> items = dict.Where((kvp) => predicate(kvp.Value))
				.Select((kvp) => kvp.Key).ToList();
  }
}
";

		private const string WrongMoqStyle = @"
using System;

public class It
{
  public static T Is<T>(System.Func<T, bool> predicate) => default(T);
}

public class Times
{
  public static Times Once => new Times();
}

public interface ICertificateInfo
{
  string SerialNumber { get; }
}

public interface IMockProvider
{
  void StoreCertificate(ICertificateInfo cert);
  void Verify<T>(System.Func<IMockProvider, T> expression, Times times);
}

public class TestClass
{
  private IMockProvider _mockProvider;
  private ICertificateInfo _embeddedPrimaryClient;

  public void Method()
  {
    _mockProvider.Verify(x => x.StoreCertificate(It.Is<ICertificateInfo>(c => c.SerialNumber == _embeddedPrimaryClient.SerialNumber)), Times.Once);
  }
}
";

		private const string CorrectMoqStyle = @"
using System;

public class It
{
  public static T Is<T>(System.Func<T, bool> predicate) => default(T);
}

public class Times
{
  public static Times Once => new Times();
}

public interface ICertificateInfo
{
  string SerialNumber { get; }
}

public interface IMockProvider
{
  void StoreCertificate(ICertificateInfo cert);
  void Verify<T>(System.Func<IMockProvider, T> expression, Times times);
}

public class TestClass
{
  private IMockProvider _mockProvider;
  private ICertificateInfo _embeddedPrimaryClient;

  public void Method()
  {
    _mockProvider.Verify(x => x.StoreCertificate(It.Is<ICertificateInfo>
      (c => c.SerialNumber == _embeddedPrimaryClient.SerialNumber)), Times.Once);
  }
}
";

		[DataTestMethod]
		[DataRow(WrongMultiple, CorrectMultiple, DisplayName = nameof(WrongMultiple)),
		 DataRow(WrongDistinct, CorrectDistinct, DisplayName = nameof(WrongDistinct)),
		 DataRow(WrongParenthesized, CorrectParenthesized, DisplayName = nameof(WrongParenthesized)),
		 DataRow(WrongWithIndentationIssue, CorrectWithProperIndentation, DisplayName = nameof(WrongWithIndentationIssue)),
		 DataRow(WrongMoqStyle, CorrectMoqStyle, DisplayName = nameof(WrongMoqStyle))]
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
