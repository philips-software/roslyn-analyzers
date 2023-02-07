// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CatchesNestedStringFormat()
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

			VerifyDiagnostic(template, DiagnosticId.NoNestedStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CatchesNestedInterpolatedStringFormat()
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

			VerifyDiagnostic(template, DiagnosticId.NoNestedStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CatchesNestedInterpolatedStringFormatCustomMethod()
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

			VerifyDiagnostic(template, DiagnosticId.NoNestedStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly2()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[DataRow("{0}")]
		[DataRow("{1}")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly2a(string arg)
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly3()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly3a()
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

			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly4()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}



		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly5()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly6()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly6a()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselesslyFromStringMethod()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselesslyFromIntField()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselesslyFromLocal()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselesslyFromParameter()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselesslyFromOutParameter()
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[DataRow("\"{0}\"", ", errorMessage", true)]
		[DataRow("\"this is a test\"", "", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselesslyLogStatement(string format, string args, bool isError)
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
				VerifyDiagnostic(code, DiagnosticId.NoUnnecessaryStringFormats);
			}
			else
			{
				VerifySuccessfulCompilation(code);
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
			VerifySuccessfulCompilation(string.Format(template, format));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly6b()
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

			VerifySuccessfulCompilation(template);
		}



		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontStringFormatUselessly6c()
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

			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CatchesNoNestedStringFormat()
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

			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CatchesNoNestedStringFormat2()
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

			VerifySuccessfulCompilation(template);
		}

		[DataRow(@"$""{Environment.NewLine}""")]
		[DataRow(@"string.Format(""{0}"", Environment.NewLine)")]
		[DataRow(@"string.Format(""{0}"", new object[] { Environment.NewLine })")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ErrorsOnPropertyLikeStrings(string argument)
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

			VerifyDiagnostic(template, DiagnosticId.NoUnnecessaryStringFormats);
		}

		[DataRow(@"$""{Test()}""")]
		[DataRow(@"$""{4}""")]
		[DataRow(@"$""{4:x}""")]
		[DataRow(@"$""this is a test {Environment.NewLine}""")]
		[DataRow(@"string.Format(""This is a test {0}"", Environment.NewLine)")]
		[DataRow(@"string.Format(""This is a test {0}"", new object[] { Environment.NewLine })")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IgnoresFormatStringsWithAdditionalText(string argument)
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

			VerifySuccessfulCompilation(template);
		}
	}
}
