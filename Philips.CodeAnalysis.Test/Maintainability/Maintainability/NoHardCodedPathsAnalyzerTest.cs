// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class NoHardCodedPathsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NoHardCodedPathsAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesHardCodedAbsoluteWindowsPaths()
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
			await VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesHardCodedAbsoluteWindowsPathWithDoubleSlash()
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
			await VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths).ConfigureAwait(false);

		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesHardCodedPathsRegardlessOfCase()
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
			await VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesHardCodedPathsAsUnc()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""\\server\share\BIN\EXAMPLE.XML"";
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesHardCodedPaths2()
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
			await VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesHardCodedPathsWithSpace()
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

			await VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesHardCodedPathsInLongStringLiterals()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		string path = @""c:\users\test in a veeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeery long string literal"";
	}
}
";

			await VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesHardCodedPathsWithSpecialCharacters()
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

			await VerifyDiagnostic(template, DiagnosticId.NoHardcodedPaths).ConfigureAwait(false);

		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotCatchNormalString()
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

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotCatchShortString()
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

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotCatchEmptyString()
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

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotCatchRelativePath()
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
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotCatchPathsInComments()
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
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotCatchPathsInTestCode()
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
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);

		}
	}
}
