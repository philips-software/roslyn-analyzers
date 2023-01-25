// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
		public void DefaultVersionIs1000()
		{
			VerifySuccessfulCompilation(TestCode);
		}
	}
}
