// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class NoHardCodedPathsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NoHardCodedPathsAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths);

		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
			VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths);

		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifySuccessfulCompilation(template);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DoesNotCatchShortString()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""x"";
	}
}
";

			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DoesNotCatchEmptyString()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @"""";
	}
}
";

			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DoesNotCatchRelativePath()
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
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DoesNotCatchPathsInComments()
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
			VerifySuccessfulCompilation(template);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DoesNotCatchPathsInTestCode()
		{
			const string template = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo
{
	[TestMethod]
    public void Test()
	{
		string path = @""c:\users\Bin\example.xml"";
	}
}
";
			VerifySuccessfulCompilation(template);

		}
	}
}
