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

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidInvocationAsArgumentAnalyzerTest : CodeFixVerifier
	{

		[TestMethod]
		public void AvoidInvocationAsArgumentTest()
		{
			string template = @"
class Foo
{{
  public Foo() : base(Do()) {}
  public Foo(string h) {}
  public static string Do() {{ return ""hi"";}}
  public string Moo(string s) {{ return ""hi"";}}

  public void MyTest()
  {{
    string.Format(Foo.Do());
    string.Format(nameof(MyTest));
    string.Format(5.ToString());
    string.Format(Moo(Do()));	  // Finding
    Assert.Format(Moo("""").Format(""""));
  }}
}}
";
			var results = new[] { new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidInvocationAsArgument),
						Message = new Regex(".*"),
						Severity = DiagnosticSeverity.Error,
						Locations = new[]
						{
							new DiagnosticResultLocation("Test0.cs", 14, 19)
						}
					}
				};

			VerifyCSharpDiagnostic(template, results);
		}

		[TestMethod]
		public void AvoidInvocationAsArgumentReturnTest()
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
    var resultOfDo = Do();
    return Moo(resultOfDo);
  }}
}}
";
			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyCSharpFix(errorContent, fixedContent);
		}

		[TestMethod]
		public void AvoidInvocationAsArgumentFixTest()
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
     var resultOfDo = Do();
     Moo(resultOfDo);
  }
}
";

			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyCSharpFix(errorContent, fixedContent);
		}

		[TestMethod]
		public void AvoidInvocationAsArgumentLocalAssignmentTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     string x = Moo(Do());
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
     var resultOfDo = Do();
     string x = Moo(resultOfDo);
  }
}
";

			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyCSharpFix(errorContent, fixedContent);
		}

		[TestMethod]
		public void AvoidInvocationAsArgumentAssignmentTest()
		{
			string errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     string x;
     x = Moo(Do());
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
     string x;
     var resultOfDo = Do();
     x = Moo(resultOfDo);
  }
}
";

			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyCSharpFix(errorContent, fixedContent);
		}


		[TestMethod]
		public void AvoidInvocationAsArgumentFixReturnTest()
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
     var resultOfDo = Do();
     return Moo(resultOfDo);
  }
}
";

			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyCSharpFix(errorContent, fixedContent);
		}

		[TestMethod]
		public void AvoidInvocationAsArgumentFixIfTest()
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
       var resultOfDo = Do();
       Moo(resultOfDo);
     }
  }
}
";
			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyCSharpFix(errorContent, fixedContent);
		}

		[TestMethod]
		public void AvoidInvocationAsArgumentFixNewTest()
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
     var resultOfDo = Do();
     new Meow(resultOfDo);
  }
}
";
			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyCSharpFix(errorContent, fixedContent);
		}

		[TestMethod]
		public void AvoidInvocationAsArgumentFixSwitchTest()
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
       case 0: var resultOfDo = Do(); Moo(resultOfDo); break;
     }
  }
}
";
			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyCSharpFix(errorContent, fixedContent);
		}

		[TestMethod]
		public void AvoidInvocationAsArgumentFixExpressionTest()
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

			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			//VerifyCSharpFix(errorContent, fixedContent);
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new AvoidInvocationAsArgumentCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AvoidInvocationAsArgumentAnalyzer();
        }
    }
}