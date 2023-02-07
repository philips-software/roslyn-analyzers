// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	[TestClass]
	public class AvoidAssemblyVersionChangeAnalyzerTest2 : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestableAvoidAssemblyVersionChangeAnalyzer(CorrectReturnedVersion);
		}

		private const string TestCode = @"
class Foo 
{
}
";

		private const string CorrectReturnedVersion = "1.0.0.0";

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DefaultVersionIs1000Async()
		{
			await VerifySuccessfulCompilation(TestCode).ConfigureAwait(false);
		}
	}
}
