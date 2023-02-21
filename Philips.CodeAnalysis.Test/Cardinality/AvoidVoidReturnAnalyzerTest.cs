// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NotFireForEmptyFiles()
		{
			string test = "";

			await VerifySuccessfulCompilation(test);
		}

		//No diagnostics expected to show up
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NotFireForNonVoidPredefinedReturnTypes()
		{
			string test = @"
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NotFireForNonVoidMethods()
		{
			string test = @"
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FireForVoidReturn()
		{
			string test = @"
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NotFireForOverriddenVoidReturn()
		{
			string test = @"
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
