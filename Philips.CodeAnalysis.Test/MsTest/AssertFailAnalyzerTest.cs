// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

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
		public void FlagAssertFailInGuardedIfStatement()
		{
			VerifyError(@"
bool isDone = false;
if(!isDone)
{
	Assert.Fail();
}
", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}

		[TestMethod]
		public void FlagAssertFailInGuardedElseStatement()
		{
			VerifyError(@"
bool isDone = false;
if(!isDone)
{
}
else
{
	Assert.Fail();
}
", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}

		[TestMethod]
		public void FlagAssertFailInGuardedElseStatementWithoutBraces()
		{
			VerifyError(@"
bool isDone = false;
if(!isDone)
{
}
else
	Assert.Fail();
", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}

		[TestMethod]
		public void IgnoreAssertFailInCatchBlock()
		{
			VerifyNoError(@"
try 
{
}
catch
{
	Assert.Fail();
}
");
		}

		[TestMethod]
		public void FlagAssertFailInTryBlock()
		{
			VerifyError(@"
try 
{
	Assert.Fail();
}
catch
{
}
", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}

		[TestMethod]
		public void IgnoreAssertInForeach()
		{
			VerifyNoError(@"
foreach(var foo in Array.Empty<int>())
{
	System.Console.WriteLine(foo.ToString());
	Assert.Fail();
}
");
		}

		[TestMethod]
		public void FlagAssertInEmptyForeach()
		{
			VerifyError(@"
foreach(var foo in Array.Empty<int>())
{
	Assert.Fail();
}
", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}

		[TestMethod]
		public void FlagAssertInEmptyForeachNoBraces()
		{
			VerifyError(@"
foreach(var foo in Array.Empty<int>())
	Assert.Fail();

", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}

		[TestMethod]
		public void IgnoreAssertInEmptyUsing()
		{
			VerifyNoError(@"
IDisposable foo = null;
using(foo)
{
	Assert.Fail();
}
");
		}

		[TestMethod]
		public void FlagAssertEndingForeachLoop()
		{
			VerifyError(@"
foreach(var foo in Array.Empty<int>())
{
	if(foo == 0)
	{
		continue;
	}

	Assert.Fail();
}
", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}

		[TestMethod]
		public void FlagAssertEndingForeachLoopNoBraces()
		{
			VerifyError(@"
foreach(var foo in Array.Empty<int>())
{
	if(foo == 0)
		continue;

	Assert.Fail();
}
", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}

		[TestMethod]
		public void FlagAssertEndingForeachLoopElse()
		{
			VerifyError(@"
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
", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}

		[TestMethod]
		public void FlagAssertEndingForeachLoopNoBracesElse()
		{
			VerifyError(@"
foreach(var foo in Array.Empty<int>())
{
	if(foo == 0)
		continue;
	else
		Assert.Fail();
}
", Helper.ToDiagnosticId(DiagnosticIds.AssertFail));
		}
		#endregion
	}
}
