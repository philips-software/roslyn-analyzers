using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Cardinality;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Cardinality
{
	[TestClass]
	public class AvoidVoidReturnAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidVoidReturnAnalyzer();
		}

		//No diagnostics expected to show up
		[TestMethod]
		public async Task NotFireForEmptyFiles()
		{
			var test = "";

			await VerifySuccessfulCompilation(test);
		}

		//No diagnostics expected to show up
		[TestMethod]
		public async Task NotFireForNonVoidPredefinedReturnTypes()
		{
			var test = @"
    namespace ConsoleApplication1
    {
        class MyClass
        {   
            public bool Foo() { return true; }
        }
    }";

			await VerifySuccessfulCompilation(test);
		}

		[TestMethod]
		public async Task NotFireForNonVoidMethods()
		{
			var test = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
        }

        public class Meow
        {
            public Foo Hi()
            {
                return new Foo();
            }
        }
    }";

			await VerifySuccessfulCompilation(test);
		}

		[TestMethod]
		public async Task FireForVoidReturn()
		{
			var test = @"
    namespace ConsoleApplication1
    {
        class MyClass
        {   
            public void Foo() {}
        }
    }";

			await VerifyDiagnostic(test, regex: "Foo");
		}


		// It's impossible to avoid void methods when they override those of base class
		[TestMethod]
		public async Task NotFireForOverriddenVoidReturn()
		{
			var test = @"
    namespace ConsoleApplication1
    {
        public abstract class AbstractBase
        {
            public abstract void Foo(int param); // Will fire here
        }

        public class Unavoidable : AbstractBase
        {
            public override void Foo(int param) // But not here
            {
                throw new System.NotImplementedException();
            }
        }
    }";

			await VerifyDiagnostic(test, regex: "Foo", line: 6);
		}
	}
}
