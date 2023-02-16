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
		public async Task CatchesHardCodedAbsoluteWindowsPathsAsync()
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
		public async Task CatchesHardCodedAbsoluteWindowsPathWithDoubleSlashAsync()
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
		public async Task CatchesHardCodedPathsRegardlessOfCaseAsync()
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
		public async Task CatchesHardCodedPaths2Async()
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
		public async Task CatchesHardCodedPathsWithSpaceAsync()
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
		public async Task CatchesHardCodedPathsWithSpecialCharactersAsync()
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
		public async Task DoesNotCatchNormalStringAsync()
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
		public async Task DoesNotCatchShortStringAsync()
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
		public async Task DoesNotCatchEmptyStringAsync()
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
		public async Task DoesNotCatchRelativePathAsync()
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
		public async Task DoesNotCatchPathsInCommentsAsync()
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
		public async Task DoesNotCatchPathsInTestCodeAsync()
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
