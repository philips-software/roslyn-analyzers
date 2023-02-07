// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Serialization;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Verifiers;
using Philips.CodeAnalysis.Test.Helpers;
using System.Threading.Tasks;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidInvocationAsArgumentAnalyzerTest : CodeFixVerifier
	{

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentTestAsync()
		{
			string template = @"
class Foo
{{
  public Foo() : base(Do()) {}
  public Foo(string h) {}
  public static string Do() {{ return ""hi"";}}
  public string Moo(string s) {{ return ""hi"";}}
  public string ToString() {{ return ""hi"";}}
  public string ToList() {{ return ""hi"";}}
  public string ToArray() {{ return ""hi"";}}

  public void MyTest()
  {{
    string.Format(Foo.Do());
    string.Format(Foo.ToString());
    string.Format(Foo.ToList());
    string.Format(Foo.ToArray());
    string.Format(nameof(MyTest));
    string.Format(5.ToString());
    string.Format(Moo(Do()));	  // Finding
    Assert.Format(Moo("""").Format(""""));
  }}
}}
";
			var result = new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AvoidInvocationAsArgument),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 20, 19)
				}
			};

			await VerifyDiagnostic(template, result).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentReturnTest()
		{
			string errorContent = @"
class Foo
{{
  public string Do() {{ return ""hi"";}}
  public string Moo(string s) {{ return ""hi"";}}

  public string MyTest()
  {{
    return Moo(Do());
  }}
}}
";

			string fixedContent = @"
class Foo
{{
  public string Do() {{ return ""hi"";}}
  public string Moo(string s) {{ return ""hi"";}}

  public string MyTest()
  {{
    var s = Do();
    return Moo(s);
  }}
}}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsUnknownSymbolArgumentReturnTest()
		{
			string errorContent = @"
class Foo
{{
  public string Moo(string s) {{ return ""hi"";}}

  public string MyTest()
  {{
    return Moo(Do());
  }}
}}
";

			string fixedContent = @"
class Foo
{{
  public string Moo(string s) {{ return ""hi"";}}

  public string MyTest()
  {{
    var resultOfDo = Do();
    return Moo(resultOfDo);
  }}
}}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     Moo(Do());
  }
}
";
			string fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     var x = Do();
     Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationInIfStatementTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public bool Moo(string x) { }
  public void MyTest()
  {
     if (Moo(Do()))
       { var y = 1; }
  }
}
";
			string fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public bool Moo(string x) { }
  public void MyTest()
  {
     var x = Do();
     if (Moo(x))
       { var y = 1; }
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationInWhileStatementTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public bool Moo(string x) { }
  public void MyTest()
  {
     while (Moo(Do()))
       { var y = 1; }
  }
}
";
			string fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public bool Moo(string x) { }
  public void MyTest()
  {
     var x = Do();
     while (Moo(x))
       { var y = 1; }
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsMemberAccessArgumentFixTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     Moo(this.Do());
  }
}
";
			string fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     var x = this.Do();
     Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsStaticMemberAccessArgumentFixTest()
		{
			string errorContent = @"
class Foo
{
  public static string Do() { return ""hi"";}
  public static void Moo(string value) { }
  public void MyTest()
  {
     Foo.Moo(Do());
  }
}
";
			string fixedContent = @"
class Foo
{
  public static string Do() { return ""hi"";}
  public static void Moo(string value) { }
  public void MyTest()
  {
     var resultOfDo = Do();
     Foo.Moo(resultOfDo);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentLocalAssignmentTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     string y = Moo(Do());
  }
}
";
			string fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     var x = Do();
     string y = Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentAssignmentTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     string y;
     y = Moo(Do());
  }
}
";
			string fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     string y;
     var x = Do();
     y = Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixReturnTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public string MyTest()
  {
     // Comment
     return Moo(Do());
  }
}
";
			string fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public string MyTest()
  {
     // Comment
     var x = Do();
     return Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixIfTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     if (true)
       Moo(Do());
  }
}
";
			string fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     if (true)
     {
       var x = Do();
       Moo(x);
     }
  }
}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixNewTest()
		{
			string errorContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void MyTest()
  {
     new Meow(Do());
  }
}
";
			string fixedContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void MyTest()
  {
     var x = Do();
     new Meow(x);
  }
}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixSwitchTest()
		{
			string errorContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     int i;
     switch (i)
     {
       case 0: Moo(Do()); break;
     }
  }
}
";
			string fixedContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     int i;
     switch (i)
     {
       case 0: var x = Do(); Moo(x); break;
     }
  }
}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixExpressionTestAsync()
		{
			string errorContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest() => Moo(Do());
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidInvocationAsArgumentCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
        {
            return new AvoidInvocationAsArgumentAnalyzer();
        }
    }
}