using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class NoNestedStringFormatsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new NoNestedStringFormatsAnalyzer();
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoNestedStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoNestedStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoNestedStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[DataRow("{0}")]
		[DataRow("{1}")]
		[DataTestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template);
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}



		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[DataRow("\"{0}\"", ", errorMessage", true)]
		[DataRow("\"this is a test\"", "", false)]
		[DataTestMethod]
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
			DiagnosticResult[] expected = Array.Empty<DiagnosticResult>();
			if (isError)
			{
				expected = new[] { DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats) };
			}

			VerifyCSharpDiagnostic(string.Format(template, format, args), expected);
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template);
		}



		[TestMethod]
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

			VerifyCSharpDiagnostic(template);
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template);
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(template);
		}

		[DataRow(@"$""{Environment.NewLine}""")]
		[DataRow(@"string.Format(""{0}"", Environment.NewLine)")]
		[DataRow(@"string.Format(""{0}"", new object[] { Environment.NewLine })")]
		[DataTestMethod]
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoUnnecessaryStringFormats));
		}

		[DataRow(@"$""{Test()}""")]
		[DataRow(@"$""{4}""")]
		[DataRow(@"$""{4:x}""")]
		[DataRow(@"$""this is a test {Environment.NewLine}""")]
		[DataRow(@"string.Format(""This is a test {0}"", Environment.NewLine)")]
		[DataRow(@"string.Format(""This is a test {0}"", new object[] { Environment.NewLine })")]
		[DataTestMethod]
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

			VerifyCSharpDiagnostic(template);
		}
	}
}
