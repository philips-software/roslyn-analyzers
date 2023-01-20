// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidEmptyStatementBlockAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidEmptyStatementBlocksAnalyzer();
		}
		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidEmptyStatementBlocksCodeFixProvider();
		}

		[TestMethod]
		public void FixAllProviderTest()
		{
			Assert.AreEqual(WellKnownFixAllProviders.BatchFixer, GetCodeFixProvider().GetFixAllProvider());
		}

		[TestMethod]
		public void CatchesEmptyPrivateMethod()
		{
			const string template = @"
using System;
class Foo
{
	private void Test()
	{
	
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidEmptyStatementBlock));

		}

		[TestMethod]
		public void CatchesEmptyStatementMethod()
		{
			const string template = @"
using System;
class Foo
{
	private void Test()
	{
		Console.WriteLine(""hi""); ; // stuff
	}
}
";
			const string fixedCode = @"
using System;
class Foo
{
	private void Test()
	{
		Console.WriteLine(""hi"");  // stuff
	}
}
";

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidEmptyStatement));
			VerifyCSharpFix(template, fixedCode);
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
	private void Test()
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


		[TestMethod]
		public void DoesNotFailOnEmptyConstructor()
		{
			const string template = @"
using System;
class Foo
{
	public Foo()
	{ }
}
";
			VerifyCSharpDiagnostic(template);
		}

		[TestMethod]
		public void ConstructorWithEmptyStatementBlockFails()
		{
			const string template = @"
using System;
class Foo
{
	public Foo()
	{
		{}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidEmptyStatementBlock));
		}

		[TestMethod]
		public void EmptyCatchBlockFails()
		{
			const string template = @"
using System;
class Foo
{
	public void Meow()
	{
		try
		{
			Console.WriteLine(0);
		}
		catch (Exception)
		{
		}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidEmptyCatchBlock));
		}


		[TestMethod]
		public void EmptyPublicMethodAllowed()
		{
			const string template = @"
using System;

class Foo
{
	public void Meow()
	{
	}
}
";
			VerifyCSharpDiagnostic(template);
		}


		[TestMethod]
		public void EmptyProtectedMethodAllowed()
		{
			const string template = @"
using System;

class Foo
{
	protected void Meow()
	{
	}
}
";
			VerifyCSharpDiagnostic(template);
		}


		[TestMethod]
		public void ParenthesizedLambdasAllowed()
		{
			const string template = @"
using System;

class Foo
{
	public void Meow()
	{
		Action a = () => { };
	}
}
";
			VerifyCSharpDiagnostic(template);
		}

		[TestMethod]
		public void SimpleLambdasAllowed()
		{
			const string template = @"
using System;

class Foo
{
	public void Meow()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		_ = serviceCollection.AddDatabaseConnection(options => { });
	}
}
";
			VerifyCSharpDiagnostic(template);
		}

		[TestMethod]
		public void EmptyLockBlocksAllowed()
		{
			const string template = @"
using System;

class Foo
{
	public void Meow()
	{
			object l = new object();
			lock (l) { }
	}
}
";
			VerifyCSharpDiagnostic(template);
		}


		[TestMethod]
		public void PublicMethodWithEmptyStatementBlockFails()
		{
			const string template = @"
using System;

class Foo
{
	public void Meow()
	{
		{}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidEmptyStatementBlock));
		}

	}
}
