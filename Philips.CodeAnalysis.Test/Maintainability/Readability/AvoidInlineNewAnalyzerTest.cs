// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.


using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class AvoidInlineNewAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidInlineNewAnalyzer();
		}

		private string CreateFunction(string content)
		{
			string baseline = @"
class Foo 
{{
  public void Foo()
  {{
    {0};
  }}
}}
";

			return string.Format(baseline, content);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorOnAllowedMethodsAsync()
		{
			var file = CreateFunction("string str = new object().ToString()");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontInlineNewCallAsync()
		{
			var file = CreateFunction("int hash = new object().GetHashCode()");
			await VerifyAsync(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorIfPlacedInLocalAsync()
		{
			var file = CreateFunction("object obj = new object(); string str = obj.ToString();");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorIfPlacedInFieldAsync()
		{
			var file = CreateFunction("_obj = new object(); string str = _obj.ToString();");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[DataRow("new Foo()")]
		[DataRow("(new Foo())")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DontInlineNewCallCustomTypeAsync(string newVariant)
		{
			var file = CreateFunction($"int hash = {newVariant}.GetHashCode()");
			await VerifyAsync(file).ConfigureAwait(false);
		}

		[DataRow("new Foo()")]
		[DataRow("(new Foo())")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorInlineNewCallCustomTypeAllowedMethodAsync(string newVariant)
		{
			var file = CreateFunction($"string str = {newVariant}.ToString()");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorIfPlacedInLocalCustomTypeAsync()
		{
			var file = CreateFunction("object obj = new Foo(); string str = obj.ToString();");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorIfPlacedInFieldCustomTypeAsync()
		{
			var file = CreateFunction("_obj = new Foo(); string str = _obj.ToString();");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorIfPlacedInContainerAsync()
		{
			var file = CreateFunction("var v = new List<object>(); v.Add(new object());");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorIfReturnedAsync()
		{
			var file = CreateFunction("return new object();");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ErrorIfReturnedAsync()
		{
			var file = CreateFunction("return new object().GetHashCode();");
			await VerifyAsync(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorIfReturnedAllowedMethodAsync()
		{
			var file = CreateFunction("return new object().ToString();");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorIfThrownAsync()
		{
			var file = CreateFunction("throw new Exception();");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ErrorIfThrownAsync()
		{
			var file = CreateFunction("throw new object().Foo;");
			await VerifyAsync(file).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoErrorOnAsSpanMethodAsync()
		{
			var file = CreateFunction("new string(\"\").AsSpan();");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		private async Task VerifyAsync(string file)
		{
			await VerifyDiagnostic(file).ConfigureAwait(false);
		}
	}
}
