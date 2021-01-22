using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class NoHardcodedPathsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new NoHardCodedPathsAnalyzer();
		}

		[TestMethod]
		public void CatchesHardCodedAbsoluteWindowsPaths()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""c:\users\Bin\example.xml"";
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoHardcodedPaths));

		}

		[TestMethod]
		public void CatchesHardCodedAbsoluteLinuxPaths()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""/boot/grub/grub.conf"";
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoHardcodedPaths));

		}

		[TestMethod]
		public void CatchesHardCodedAbsoluteWindowsPathWithDoubleSlash()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""c:\\users\\Bin\\example.xml"";
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoHardcodedPaths));

		}


		[TestMethod]
		public void CatchesHardCodedPathsRegardlessOfCase()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""C:\USERS\BIN\EXAMPLE.XML"";
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoHardcodedPaths));

		}

		[TestMethod]
		public void CatchesHardCodedPaths2()
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
		string t = @""c:\users\Bin\example.xml"";
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoHardcodedPaths));

		}

		[TestMethod]
		public void CatchesHardCodedPathsWithSpace()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""c:\users\test first\Bin\example.xml"";
	}
}
";

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoHardcodedPaths));

		}

		[TestMethod]
		public void CatchesHardCodedPathsWithSpecialCharacters()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""c:\users\test_first-second.third@fourth\Bin\example.xml"";
	}
}
";

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.NoHardcodedPaths));

		}
		[TestMethod]
		public void DoesNotCatchNormalString()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""Test"";
	}
}
";

			VerifyCSharpDiagnostic(template);

		}


		[TestMethod]
		public void DoesnotCatchRelativePath()
		{
			const string template = @"
using system;
class foo
{
	public void test()
	{
		string path = @""..\..\bin\example.xml"";
	}
}
";
			VerifyCSharpDiagnostic(template);
		}

		[TestMethod]
		public void DoesnotCatchPathsInComments()
		{
			const string template = @"
using System;
class Foo
{
	/*
	* This is a test: c:\users\Bin\example.xml
	*/
	public void Test()
	{
		string path = @""This is a test"";
	}
}
";
			VerifyCSharpDiagnostic(template);

		}

	}
}
