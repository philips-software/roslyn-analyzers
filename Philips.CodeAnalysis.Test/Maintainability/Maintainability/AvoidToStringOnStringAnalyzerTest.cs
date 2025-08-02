// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidToStringOnStringAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidToStringOnStringAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidToStringOnStringCodeFixProvider();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidToStringOnStringVariable()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		string str = ""hello"";
		System.Console.WriteLine(str.ToString());
	}
}";

			var fixedCode = @"
class TestClass
{
	public void TestMethod()
	{
		string str = ""hello"";
		System.Console.WriteLine(str);
	}
}";

			await VerifyDiagnostic(test).ConfigureAwait(false);
			await VerifyFix(test, fixedCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidToStringOnStringLiteral()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		System.Console.WriteLine(""hello"".ToString());
	}
}";

			var fixedCode = @"
class TestClass
{
	public void TestMethod()
	{
		System.Console.WriteLine(""hello"");
	}
}";

			await VerifyDiagnostic(test).ConfigureAwait(false);
			await VerifyFix(test, fixedCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidToStringOnStringExpression()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		System.Console.WriteLine((""hello"" + ""world"").ToString());
	}
}";

			var fixedCode = @"
class TestClass
{
	public void TestMethod()
	{
		System.Console.WriteLine((""hello"" + ""world""));
	}
}";

			await VerifyDiagnostic(test).ConfigureAwait(false);
			await VerifyFix(test, fixedCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowToStringOnNonStringTypes()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		int number = 42;
		string result = number.ToString();
		
		object obj = ""hello"";
		string result2 = obj.ToString();
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowToStringWithParameters()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		string str = ""hello"";
		string result = str.ToString(System.Globalization.CultureInfo.InvariantCulture);
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidToStringOnStringProperty()
		{
			var test = @"
class TestClass
{
	public string MyString { get; set; } = ""test"";
	
	public void TestMethod()
	{
		System.Console.WriteLine(MyString.ToString());
	}
}";

			var fixedCode = @"
class TestClass
{
	public string MyString { get; set; } = ""test"";
	
	public void TestMethod()
	{
		System.Console.WriteLine(MyString);
	}
}";

			await VerifyDiagnostic(test).ConfigureAwait(false);
			await VerifyFix(test, fixedCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidToStringOnStringMethodReturn()
		{
			var test = @"
class TestClass
{
	public string GetString() => ""hello"";
	
	public void TestMethod()
	{
		System.Console.WriteLine(GetString().ToString());
	}
}";

			var fixedCode = @"
class TestClass
{
	public string GetString() => ""hello"";
	
	public void TestMethod()
	{
		System.Console.WriteLine(GetString());
	}
}";

			await VerifyDiagnostic(test).ConfigureAwait(false);
			await VerifyFix(test, fixedCode).ConfigureAwait(false);
		}
	}
}
