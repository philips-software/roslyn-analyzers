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

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidInvocationAsArgumentAnalyzerTest : CodeFixVerifier
	{

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidInvocationAsArgumentTest()
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
			var results = new[] { new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidInvocationAsArgument),
						Message = new Regex(".*"),
						Severity = DiagnosticSeverity.Error,
						Locations = new[]
						{
							new DiagnosticResultLocation("Test0.cs", 20, 19)
						}
					}
				};

			VerifyDiagnostic(template, results);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
    var s = Do();
    return Moo(s);
  }}
}}
";
			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidInvocationAsUnknownSymbolArgumentReturnTest()
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
			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
     var x = Do();
     Moo(x);
  }
}
";

			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidInvocationInIfStatementTest()
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

			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidInvocationInWhileStatementTest()
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

			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidInvocationAsMemberAccessArgumentFixTest()
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

			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidInvocationAsStaticMemberAccessArgumentFixTest()
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

			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidInvocationAsArgumentLocalAssignmentTest()
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

			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidInvocationAsArgumentAssignmentTest()
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

			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
     var x = Do();
     return Moo(x);
  }
}
";

			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
       var x = Do();
       Moo(x);
     }
  }
}
";
			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
     var x = Do();
     new Meow(x);
  }
}
";
			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
       case 0: var x = Do(); Moo(x); break;
     }
  }
}
";
			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			VerifyFix(errorContent, fixedContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifyDiagnostic(errorContent, DiagnosticResultHelper.Create(DiagnosticIds.AvoidInvocationAsArgument));
			//VerifyFix(errorContent, fixedContent);
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