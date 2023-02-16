using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Cardinality;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Cardinality
{
	[TestClass]
	public class AvoidVoidReturnAnalyzerTest : DiagnosticVerifier
	{
		//No diagnostics expected to show up
		[TestMethod]
		public async Task NotFireForEmptyFilesAsync()
		{
			var test = "";

			await VerifySuccessfulCompilation(test);
		}

		//No diagnostics expected to show up
		[TestMethod]
		public async Task NotFireForNonVoidMethodsAsync()
		{
			var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

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
		public async Task FireForVoidReturnAsync()
		{
			var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class MyClass
        {   
            public void Foo() {}
        }
    }";

			await VerifyDiagnostic(test, regex: "Foo");
		}

		/**
         * The reasoning behind this condition is that its impossible to avoid void methods
         * when they override those of base class
         */
		[TestMethod]
		public async Task NotFireForOverridenVoidReturnAsync()
		{
			var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

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
			await VerifyDiagnostic(test, regex: "Foo");
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidVoidReturnAnalyzer();
		}
	}
}
