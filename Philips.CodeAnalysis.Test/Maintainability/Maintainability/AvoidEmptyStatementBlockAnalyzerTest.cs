// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidEmptyStatementBlockAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidEmptyStatementBlocksAnalyzer();
		}
		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidEmptyStatementBlocksCodeFixProvider();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FixAllProviderTest()
		{
			Assert.AreEqual(WellKnownFixAllProviders.BatchFixer, GetCodeFixProvider().GetFixAllProvider());
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesEmptyPrivateMethodAsync()
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
			await VerifyDiagnostic(template, DiagnosticId.AvoidEmptyStatementBlock).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesEmptyStatementMethod()
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

			await VerifyDiagnostic(template, DiagnosticId.AvoidEmptyStatement).ConfigureAwait(false);
			await VerifyFix(template, fixedCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesEmptyStatementBlockAsync()
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
			await VerifyDiagnostic(template, DiagnosticId.AvoidEmptyStatementBlock).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchesStatementBlockWithJustCommentAsync()
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
			await VerifyDiagnostic(template, DiagnosticId.AvoidEmptyStatementBlock).ConfigureAwait(false);


		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotCatchStatementBlockAsync()
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
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);



		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotFailOnEmptyConstructorAsync()
		{
			const string template = @"
using System;
class Foo
{
	public Foo()
	{ }
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ConstructorWithEmptyStatementBlockFailsAsync()
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
			await VerifyDiagnostic(template, DiagnosticId.AvoidEmptyStatementBlock).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyCatchBlockFailsAsync()
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
			await VerifyDiagnostic(template, DiagnosticId.AvoidEmptyCatchBlock).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyPublicMethodAllowedAsync()
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
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyProtectedMethodAllowedAsync()
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
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParenthesizedLambdasAllowedAsync()
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
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SimpleLambdasAllowedAsync()
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
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyLockBlocksAllowedAsync()
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
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PublicMethodWithEmptyStatementBlockFailsAsync()
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
			await VerifyDiagnostic(template, DiagnosticId.AvoidEmptyStatementBlock).ConfigureAwait(false);
		}

	}
}
