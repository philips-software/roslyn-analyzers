// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.DuplicateCodeAnalyzer;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.DuplicateCode
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
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidDuplicateCodeNoError(string literal1, string literal2, bool isClass)
		{
			var testCode = CreateTestCode(literal1, literal2, isClass);
			VerifySuccessfulCompilation(testCode);
		}

		[DataTestMethod]
		[DataRow("test123", true)]
		[DataRow("test123", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidDuplicateCodeError(string literal, bool isClass)
		{
			var testCode = CreateTestCode(literal, literal, isClass);
			VerifyDiagnostic(testCode);
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
