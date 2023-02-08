// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class NoEmptyTestMethodsDiagnosticAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NoEmptyTestMethodsDiagnosticAnalyzer();
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalSourceCode()
		{
			string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class DerivedTestMethod : TestMethodAttribute
{
}

";

			return base.GetAdditionalSourceCode().Add(("DerivedTestMethod.cs", code));
		}

		[DataRow("DerivedTestMethod")]
		[DataRow("TestMethod")]
		[DataRow("DataTestMethod")]
		[DataRow("DataRow(null)")]
		[DataRow("TestInitialize")]
		[DataRow("AssemblyInitialize")]
		[DataRow("ClassInitialize")]
		[DataRow("TestCleanup")]
		[DataRow("ClassCleanup")]
		[DataRow("AssemblyCleanup")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyMethodTriggersAnalyzerAsync(string attribute)
		{
			const string template = @"using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass] public class Foo {{ [{0}] public void Method() {{ }} }}";

			await VerifyDiagnostic(string.Format(template, attribute), DiagnosticId.TestMethodsMustNotBeEmpty).ConfigureAwait(false);
		}
	}
}
