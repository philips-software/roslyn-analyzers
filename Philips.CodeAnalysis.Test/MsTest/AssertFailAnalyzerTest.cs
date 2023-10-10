// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class AssertFailAnalyzerTest : AssertDiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AssertFailAnalyzer();
		}

		#endregion

		#region Public Interface

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertFailInGuardedIfStatementAsync()
		{
			await VerifyError(@"
bool isDone = false;
if(!isDone)
{
	Assert.Fail();
}
", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertFailInGuardedElseStatementAsync()
		{
			await VerifyError(@"
bool isDone = false;
if(!isDone)
{
}
else
{
	Assert.Fail();
}
", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertFailInGuardedElseStatementWithoutBracesAsync()
		{
			await VerifyError(@"
bool isDone = false;
if(!isDone)
{
}
else
	Assert.Fail();
", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoreAssertFailInCatchBlockAsync()
		{
			await VerifyNoError(@"
try 
{
}
catch
{
	Assert.Fail();
}
").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertFailInTryBlockAsync()
		{
			await VerifyError(@"
try 
{
	Assert.Fail();
}
catch
{
}
", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoreAssertInForeachAsync()
		{
			await VerifyNoError(@"
foreach(var foo in Array.Empty<int>())
{
	System.Console.WriteLine(foo.ToString());
	Assert.Fail();
}
").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertInEmptyForeachAsync()
		{
			await VerifyError(@"
foreach(var foo in Array.Empty<int>())
{
	Assert.Fail();
}
", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertInEmptyForeachNoBracesAsync()
		{
			await VerifyError(@"
foreach(var foo in Array.Empty<int>())
	Assert.Fail();

", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoreAssertInEmptyUsingAsync()
		{
			await VerifyNoError(@"
IDisposable foo = null;
using(foo)
{
	Assert.Fail();
}
").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertEndingForeachLoopAsync()
		{
			await VerifyError(@"
foreach(var foo in Array.Empty<int>())
{
	if(foo == 0)
	{
		continue;
	}

	Assert.Fail();
}
", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertEndingForeachLoopNoBracesAsync()
		{
			await VerifyError(@"
foreach(var foo in Array.Empty<int>())
{
	if(foo == 0)
		continue;

	Assert.Fail();
}
", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertEndingForeachLoopElseAsync()
		{
			await VerifyError(@"
foreach(var foo in Array.Empty<int>())
{
	if(foo == 0)
	{
		continue;
	}
	else
	{
		Assert.Fail();
	}
}
", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FlagAssertEndingForeachLoopNoBracesElseAsync()
		{
			await VerifyError(@"
foreach(var foo in Array.Empty<int>())
{
	if(foo == 0)
		continue;
	else
		Assert.Fail();
}
", DiagnosticId.AssertFail.ToId()).ConfigureAwait(false);
		}
		#endregion
	}
}
