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
	public class AvoidEnumParametersTest : DiagnosticVerifier
	{
		//No diagnostics expected to show up
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NotFireForEmptyFiles()
		{
			var test = "";

			await VerifySuccessfulCompilation(test);
		}

		//No diagnostics expected to show up
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NotFireForNonVoidMethods()
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
            public bool Foo(bool b) { return true; }
        }
    }";

			await VerifySuccessfulCompilation(test);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FireForEnumParam()
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
        enum Day { Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday };  

        class MyClass
        {   
            public void Foo(Day d) {}
        }
    }";

			await VerifyDiagnostic(test, regex: "Method Foo.*Parameter d");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NotFireForInterfaceParam()
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
            public void Foo(IEnumerable<string> d) {}
        }
    }";

			await VerifySuccessfulCompilation(test);
		}

		/**
         * The reasoning behind this condition is that its impossible to avoid void methods
         * when they override those of base class
         */
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NotFireForProductParam()
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
        public class ProductOf
	{
		public bool Red;
        public int Blue;
	}

    public class ProductUse
    {
        public bool Foo(ProductOf param)
        {
            return true;
        }
    }
    }";
			await VerifySuccessfulCompilation(test);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidEnumParametersAnalyzer();
		}
	}
}
