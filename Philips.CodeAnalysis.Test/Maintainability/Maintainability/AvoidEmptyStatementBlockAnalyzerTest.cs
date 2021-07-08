// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidEmptyStatementBlockAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{

			return new AvoidEmptyStatementBlocksAnalyzer();
		}

		[TestMethod]
		public void CatchesEmptyStatementBlock()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
	
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidEmptyStatementBlock));

		}

		[TestMethod]
		public void CatchesEmptyStatementBlock2()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		int x = 0;
		Console.WriteLine(x);
		{
			}
	
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidEmptyStatementBlock));
		}

		[TestMethod]
		public void CatchesStatementBlockWithJustComment()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		//dsjkhfajk
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidEmptyStatementBlock));


		}

		[TestMethod]
		public void DoesNotCatchStatementBlock()
		{
			const string template = @"
using System;
class Foo
{
	public void Test()
	{
		int x = 0;
		Console.WriteLine(x);
	}
}
";
			VerifyCSharpDiagnostic(template);



		}
	}
	}
