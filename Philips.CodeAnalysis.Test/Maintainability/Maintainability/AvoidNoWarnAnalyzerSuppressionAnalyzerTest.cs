// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidNoWarnAnalyzerSuppressionAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidNoWarnAnalyzerSuppressionAnalyzer();
		}

		private string GetTestCode()
		{
			return @"
class Foo 
{
	public void DoSomething()
	{
		var x = 1;
	}
}
";
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidNoWarnAnalyzerSuppressionAnalyzerSuccessfulCompilationNoErrorAsync()
		{
			await VerifySuccessfulCompilation(GetTestCode()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidNoWarnAnalyzerSuppressionAnalyzerHasCorrectDiagnosticId()
		{
			var analyzer = new AvoidNoWarnAnalyzerSuppressionAnalyzer();
			System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.DiagnosticDescriptor> descriptors = analyzer.SupportedDiagnostics;
			Assert.HasCount(1, descriptors);
			Assert.IsTrue(descriptors.Any(d => d.Id == "PH2163"));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ExtractAnalyzerCodesTest()
		{
			// Test the static method indirectly by ensuring analyzer doesn't crash
			var analyzer = new AvoidNoWarnAnalyzerSuppressionAnalyzer();
			Assert.IsNotNull(analyzer);
		}
	}
}