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
	public class PreventUseOfGotoAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PreventUseOfGotoAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoLabeledStatementsAsync()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		test:
			Console.WriteLine();
	}
}
";
			await VerifyDiagnostic(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoLabeledStatementsWithGotoAsync()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		test:
			Console.WriteLine();
		goto test;
	}
}
";

			await VerifyDiagnostic(template, 2).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoGotoCaseAsync()
		{
			const string template = @"
using System;
class Foo
{
	public void Test(int a)
	{
		switch(a)
		{
			case 1: goto case 2;
			case 2:
				break;
		}
	}
}
";
			await VerifyDiagnostic(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoGotoDefaultAsync()
		{
			const string template = @"
using System;
class Foo
{
	public void Test(int a)
	{
		switch(a)
		{
			case 1: goto case default;
			default:
				break;
		}
	}
}
";
			await VerifyDiagnostic(template).ConfigureAwait(false);
		}
	}
}
