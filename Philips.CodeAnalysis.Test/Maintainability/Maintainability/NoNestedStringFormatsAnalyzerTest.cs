// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class NoNestedStringFormatsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NoNestedStringFormatsAnalyzer();
		}

		private async Task VerifyNoNestedAsync(string code)
		{
			await VerifyDiagnostic(code, DiagnosticId.NoNestedStringFormats).ConfigureAwait(false);
		}
		private async Task VerifyNoUnnecessaryAsync(string code)
		{
			await VerifyDiagnostic(code, DiagnosticId.NoUnnecessaryStringFormats).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesNestedStringFormatAsync()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string t = string.Format(string.Format(""{0}"", 4));
	}
}
";
			await VerifyNoNestedAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesNestedInterpolatedStringFormatAsync()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string t = string.Format($""test"", 1);
	}
}
";
			await VerifyNoNestedAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesNestedInterpolatedStringFormatCustomMethodAsync()
		{
			const string template = @"
using System;

class Foo
{
	public static void Log(int category, string format, params object[] args)
	{
	}

	public void Test()
	{
		Log(8, string.Format(""test"", 1));
	}
}
";
			await VerifyNoNestedAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselesslyAsync()
		{
			const string template = @"
using System;

class Foo
{
	public void Test()
	{
		string t = string.Format(""test"");
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly2Async()
		{
			const string template = @"
using System;

namespace Philips.Platform
{
public static class StringHelper
{
	public static string Format(string format, params object[] args)
	{
		return string.Empty;
	}
}
}

class Foo
{
	public void Test()
	{
		string t = Philips.Platform.StringHelper.Format(""test"");
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[DataRow("{0}")]
		[DataRow("{1}")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly2aAsync(string arg)
		{
			string template = $@"
using System;

class Log
{{
	public void Err(string format, params object[] args)
	{{
	}}

	public Log Platform {{ get; }}
}}

class Foo
{{
	public void Test()
	{{
		Log.Platform.Err(""{arg}"");
	}}
}}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly3Async()
		{
			const string template = @"
using System;

class Foo
{
	public void Test()
	{
		string t = string.Format(""{0}"", ""test"");
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly3aAsync()
		{
			const string template = @"
using System;

class Foo
{
	public void Test()
	{
		string t = string.Format(""{0}"", new object());
	}
}
";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly4Async()
		{
			const string template = @"
using System;

static class StringHelper
{
	public static string Format(string format, params object[] args)
	{
		return string.Empty;
	}
}

class Foo
{
	public void Test()
	{
		string t = StringHelper.Format(""{0}"", ""test"");
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}



		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly5Async()
		{
			const string template = @"
using System;

class Foo
{
	public void Test()
	{
		string t = string.Format(@""{0}"", ""test"");
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly6Async()
		{
			const string template = @"
using System;

static class StringHelper
{
	public static string Format(string format, params object[] args)
	{
		return string.Empty;
	}
}

class Foo
{
	public void Test()
	{
		string t = StringHelper.Format(@""{0}"", ""test"");
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly6aAsync()
		{
			const string template = @"
using System;

static class StringHelper
{
	public static string Format(string format, params object[] args)
	{
		return string.Empty;
	}
}

class Foo
{
	string XPath => ""tests""

	public void Test()
	{
		Foo obx = new Foo();

		string tooltip = StringHelper.Format(""{0}"", obx.XPath);
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselesslyFromStringMethodAsync()
		{
			const string template = @"
using System;

static class StringHelper
{
	public static string Format(string format, params object[] args)
	{
		return string.Empty;
	}
}

class Foo
{
	public void Test()
	{
		string text = StringHelper.Format(""{0}"", StringHelper.Format(""test string {0}"", 5));
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselesslyFromIntFieldAsync()
		{
			const string template = @"
using System;

static class StringHelper
{
	public static string Format(string format, params object[] args)
	{
		return string.Empty;
	}
}

class Foo
{
	public void Test()
	{
		string result = StringHelper.Format(""{0}"", TimeSpan.FromSeconds(5).ToString());
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselesslyFromLocalAsync()
		{
			const string template = @"
using System;

static class StringHelper
{
	public static string Format(string format, params object[] args)
	{
		return string.Empty;
	}
}

class Foo
{
	public void Test()
	{
		string error = string.Empty;
		string tooltip = StringHelper.Format(""{0}"", error);
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselesslyFromParameterAsync()
		{
			const string template = @"
using System;

static class StringHelper
{
	public static string Format(string format, params object[] args)
	{
		return string.Empty;
	}
}

class Foo
{
	public void Test(string message)
	{
		string tooltip = StringHelper.Format(""{0}"", message);
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselesslyFromOutParameterAsync()
		{
			const string template = @"
using System;

static class StringHelper
{
	public static string Format(string format, params object[] args)
	{
		return string.Empty;
	}
}

class Foo
{
	public static void Bar(out string s)
	{
		s = string.Empty;
	}

	public void Test()
	{
		Bar(out string s);
		string tooltip = StringHelper.Format(""{0}"", s);
	}
}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[DataRow("\"{0}\"", ", errorMessage", true)]
		[DataRow("\"this is a test\"", "", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselesslyLogStatementAsync(string format, string args, bool isError)
		{
			const string template = @"
using System;

public static class LogExtensions
{{
	public void Err(this Log log, string format, params object[] args)
	{{
	}}
}}

public class Log
{{
	public void Err(FormattableString fs)
	{{
	}}
}}

public static class Logs
{{
	public static Log ServiceAudit {{ get; }}
}}

class Foo
{{
	public void Test(out string errorMessage)
	{{
		errorMessage = this.ToString();
		Logs.ServiceAudit.Err({0}{1});
	}}
}}
";
			var code = string.Format(template, format, args);
			if (isError)
			{
				await VerifyNoUnnecessaryAsync(code).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(code).ConfigureAwait(false);
			}
		}

		[DataRow("$\"{0}\"")]
		[DataRow("$\"this is a test\"")]
		[DataRow("$\"{errorMessage}\"")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselesslyIssue134(string format)
		{
			const string template = @"
using System;

class Foo
{{
	public void Err(FormattableString fs)
	{{
	}}

	public void Test()
	{{
		string errorMessage = ""Some text"";
		Err({0});
	}}
}}
";
			VerifySuccessfulCompilation(string.Format(template, format)).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly6bAsync()
		{
			const string template = @"
using System;

enum DialogResult { }

static class MessageBoxHelper
{
	public static DialogResult Warn(string format, params object[] args)
	{
		return string.Empty;
	}
}

class Foo
{
	public void Test()
	{
		string t = MessageBoxHelper.Warn(""test"");
	}
}
";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}



		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontStringFormatUselessly6cAsync()
		{
			const string template = @"
using System;

class Foo
{
	public void Test()
	{
		string attribute = string.Format(""{0}0{1}"", ""test"", ""test2"");
	}
}
";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesNoNestedStringFormatAsync()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string t = string.Format(""{0}"", 4);
	}
}
";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesNoNestedStringFormat2Async()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string t = $""{4}"";
	}
}
";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[DataRow(@"$""{Environment.NewLine}""")]
		[DataRow(@"string.Format(""{0}"", Environment.NewLine)")]
		[DataRow(@"string.Format(""{0}"", new object[] { Environment.NewLine })")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ErrorsOnPropertyLikeStringsAsync(string argument)
		{
			string template = @$"
using System;
class Foo
{{
	public void Test()
	{{
		string t = {argument};
	}}
}}
";
			await VerifyNoUnnecessaryAsync(template).ConfigureAwait(false);
		}

		[DataRow(@"$""{Test()}""")]
		[DataRow(@"$""{4}""")]
		[DataRow(@"$""{4:x}""")]
		[DataRow(@"$""this is a test {Environment.NewLine}""")]
		[DataRow(@"string.Format(""This is a test {0}"", Environment.NewLine)")]
		[DataRow(@"string.Format(""This is a test {0}"", new object[] { Environment.NewLine })")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoresFormatStringsWithAdditionalTextAsync(string argument)
		{
			string template = @$"
using System;
class Foo
{{
	public void Test()
	{{}}

	public void Test2()
	{{
		string t = {argument};
	}}
}}
";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}
	}
}
