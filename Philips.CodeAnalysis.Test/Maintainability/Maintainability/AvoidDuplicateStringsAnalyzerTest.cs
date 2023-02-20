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
			for (int i = Start; i < Start + Count; i++)
			{
				_ = sb.AppendLine($"string str{i} = \"{i}\";");
			}
			_ = sb.AppendLine("}}}}}}");
			return sb;
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalSourceCode()
		{
			var code = GetLargeFileContents("Test1").ToString();
			return base.GetAdditionalSourceCode().Add(("Test1.cs", code));
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
