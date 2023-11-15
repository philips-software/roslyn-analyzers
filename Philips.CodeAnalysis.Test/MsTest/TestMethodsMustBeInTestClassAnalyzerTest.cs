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
	public class TestMethodsMustBeInTestClassAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Properties/Methods
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestMethodsMustBeInTestClassAnalyzer();
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalSourceCode()
		{
			var code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class DerivedTestMethod : TestMethod
{
}

";

			return base.GetAdditionalSourceCode().Add(("DerivedTestMethod.cs", code));
		}

		#endregion

		#region Public Interface

		[DataRow(false, "object", "", "[DerivedTestMethod]")]
		[DataRow(true, "object", "", "[TestMethod]")]
		[DataRow(true, "object", "", "[DataTestMethod]")]
		[DataRow(true, "object", "", "[AssemblyInitialize]")]
		[DataRow(true, "object", "", "[AssemblyCleanup]")]
		[DataRow(true, "object", "", "[ClassInitialize]")]
		[DataRow(true, "object", "", "[ClassCleanup]")]
		[DataRow(false, "object", "abstract", "[TestMethod]")]
		[DataRow(false, "object", "abstract", "[DataTestMethod]")]
		[DataRow(false, "object", "abstract", "[AssemblyInitialize]")]
		[DataRow(false, "object", "abstract", "[AssemblyCleanup]")]
		[DataRow(false, "object", "abstract", "[ClassInitialize]")]
		[DataRow(false, "object", "abstract", "[ClassCleanup]")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodsMustBeInTestClassAsync(bool isError, string baseClass, string classQualifier, string testType)
		{
			const string template = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

{0}
public {2} class Tests : {3}
{{
	{1}
	public void Foo() {{ }}
}}";

			await VerifySuccessfulCompilation(string.Format(template, "[TestClass]", testType, classQualifier, baseClass)).ConfigureAwait(false);

			var code = string.Format(template, "", testType, classQualifier, baseClass);
			if (isError)
			{
				await VerifyDiagnostic(code, DiagnosticId.TestMethodsMustBeInTestClass).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(code).ConfigureAwait(false);
			}
		}
		#endregion
	}
}
