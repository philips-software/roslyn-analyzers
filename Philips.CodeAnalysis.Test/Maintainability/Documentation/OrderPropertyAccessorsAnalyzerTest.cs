// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class OrderPropertyAccessorsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new OrderPropertyAccessorsAnalyzer();
		}

		[DataRow(@"{ get; set; }")]
		[DataRow(@"{ get; }")]
		[DataRow(@"{ get { return null; } }")]
		[DataRow(@"{ set {} }")]
		[DataRow(@"{ get; init; }")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task OrderValidTestsAsync(string property)
		{
			var text = $@"
public class TestClass
{{
	public string Foo {property}
}}
";

			await VerifySuccessfulCompilation(text).ConfigureAwait(false);
		}

		[DataRow(@"{ init; get; }")]
		[DataRow(@"{ set; get; }")]
		[DataRow(@"{ set{ } get{ return default; } }")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task OrderInvalidTestsAsync(string property)
		{
			var text = $@"
public class TestClass
{{
	public string Foo {property}
}}
";

			await VerifyDiagnostic(text).ConfigureAwait(false);
		}
	}
}
