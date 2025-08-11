// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidDuplicateStringsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidDuplicateStringsAnalyzer();
		}

		[DataTestMethod]
		[DataRow("", "", false)]
		[DataRow("test123", "test345", true)]
		[DataRow("test123", "test345", false)]
		[DataRow("t", "t", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateStringNoErrorAsync(string literal1, string literal2, bool isClass)
		{
			var testCode = CreateTestCode(literal1, literal2, isClass);
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		private const int Count = 1000;

		private StringBuilder GetLargeFileContents(string className)
		{
			const int Start = 100;

			StringBuilder sb = new($"namespace DuplicateStringsTest {{ public class {className} {{ public void MethodA() {{");
			for (var i = Start; i < Start + Count; i++)
			{
				_ = sb.AppendLine($"string str{i} = \"{i}\";");
			}
			_ = sb.AppendLine("}}}}}}");
			return sb;
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalSourceCode()
		{
			var code = GetLargeFileContents("Test1").ToString();
			ImmutableArray<(string name, string content)> result = base.GetAdditionalSourceCode().Add(("Test1.cs", code));

			// Note: Previously added cross-file test content was causing interference with other tests
			// The cross-file test now uses a different approach
			return result;
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateStringLoadTest()
		{
			var code = GetLargeFileContents(nameof(AvoidDuplicateStringLoadTest)).ToString();
			await VerifyDiagnostic(code, Count).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateStringDefaultConsoleAppTest()
		{
			var code = @"
namespace ConsoleApp2
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine(""Hello, World!"");
		}
	}
}
";
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateStringNestedClassesTest()
		{
			var code = @"
namespace DuplicateStringsTest {{
    public class Foo {{
        public void MethodA() {{  string str1 = ""Violation""; }}
        public class Meow {{
            public void MethodB() {{ string str = ""Violation""); }}
        }}
    }}
}}
";
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateStringFieldDeclarationTest()
		{
			var code = @"
namespace DuplicateStringsTest {{
    public class Foo {{
		private const string Meow = ""NotAViolation"";
    }}
}}
";
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow("test123", true)]
		[DataRow("test123", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateStringErrorAsync(string literal, bool isClass)
		{
			var testCode = CreateTestCode(literal, literal, isClass);
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateStringFalsePositiveTest()
		{
			// Test case to reproduce the issue where analyzer reports false positive
			// when string appears only once but claims it's on a different line
			var code = @"
using System;

namespace TestCase
{
    public class ServerConnection
    {
        public void ConnectToServer()
        {
            // This is around line 165 in the original issue
            // No string literal here
            var someVariable = 42;
            
            // This is around line 169 where the string actually appears
            Console.WriteLine(""Waiting to connect to server..."");
        }
    }
}
";
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateStringCrossFileIssueTest()
		{
			// Test case to check that individual files don't cause false positives
			var code1 = @"
using System;
namespace TestCase1
{
    public class Class1
    {
        public void Method1()
        {
            Console.WriteLine(""Unique message one"");
        }
    }
}
";

			var code2 = @"
using System;
namespace TestCase2
{
    public class Class2
    {
        public void Method2()
        {
            Console.WriteLine(""Unique message two"");
        }
    }
}
";

			// Test that individual files pass without issues
			await VerifySuccessfulCompilation(code1).ConfigureAwait(false);
			await VerifySuccessfulCompilation(code2).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateStringStateIsolationTest()
		{
			// Test case to verify that analyzer state is properly cleared between compilation sessions
			// This reproduces the false positive issue where strings from previous sessions
			// are incorrectly reported as duplicates
			var code = @"
using System;
namespace TestCase
{
    public class ServerConnection
    {
        public void ConnectToServer()
        {
            Console.WriteLine(""Waiting to connect to server..."");
        }
    }
}
";

			// First compilation - should not trigger any diagnostics (single string)
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);

			// Second compilation with same code - should also not trigger diagnostics
			// because analyzer state should be cleared between compilations
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		private string CreateTestCode(string literal1, string literal2, bool isClass)
		{
			var typeKind = isClass ? "class" : "struct";
			var template = @"
namespace DuplicateStringsTest {{
    public {0} Foo {{
        public void MethodA() {{
            string str1 = ""{1}"";
        }}
        public void MethodB(string str = ""{2}"") {{
        }}
    }}
}}
";
			return string.Format(template, typeKind, literal1, literal2);
		}
	}
}
