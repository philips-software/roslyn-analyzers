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
	public class PreventUseOfGotoAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
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

			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.GotoNotAllowed));
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

			var expected = DiagnosticResultHelper.CreateArray(DiagnosticIds.GotoNotAllowed).Append(DiagnosticIds.GotoNotAllowed);
			VerifyDiagnostic(template, expected);
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

			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.GotoNotAllowed));
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

			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.GotoNotAllowed));
		}
	}
}
