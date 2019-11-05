// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class PreventUseOfGotoAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new PreventUseOfGotoAnalyzer();
		}

		[TestMethod]
		public void NoLabeledStatements()
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.GotoNotAllowed));
		}

		[TestMethod]
		public void NoLabeledStatementsWithGoto()
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.GotoNotAllowed), DiagnosticResultHelper.Create(DiagnosticIds.GotoNotAllowed));
		}

		[TestMethod]
		public void NoGotoCase()
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.GotoNotAllowed));
		}

		[TestMethod]
		public void NoGotoDefault()
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.GotoNotAllowed));
		}
	}
}
