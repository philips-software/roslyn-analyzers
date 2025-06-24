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

		[DataRow(true, "[STATestClass]", "object", "", "[TestMethod]")]
		[DataRow(true, "[STATestClass]", "object", "", "[DataTestMethod]")]
		[DataRow(false, "[STATestClass]", "object", "abstract", "[TestMethod]")]
		[DataRow(false, "[TestClass]", "object", "", "[DerivedTestMethod]")]
		[DataRow(true, "[TestClass]", "object", "", "[TestMethod]")]
		[DataRow(true, "[TestClass]", "object", "", "[DataTestMethod]")]
		[DataRow(true, "[TestClass]", "object", "", "[AssemblyInitialize]")]
		[DataRow(true, "[TestClass]", "object", "", "[AssemblyCleanup]")]
		[DataRow(true, "[TestClass]", "object", "", "[ClassInitialize]")]
		[DataRow(true, "[TestClass]", "object", "", "[ClassCleanup]")]
		[DataRow(false, "[TestClass]", "object", "abstract", "[TestMethod]")]
		[DataRow(false, "[TestClass]", "object", "abstract", "[DataTestMethod]")]
		[DataRow(false, "[TestClass]", "object", "abstract", "[AssemblyInitialize]")]
		[DataRow(false, "[TestClass]", "object", "abstract", "[AssemblyCleanup]")]
		[DataRow(false, "[TestClass]", "object", "abstract", "[ClassInitialize]")]
		[DataRow(false, "[TestClass]", "object", "abstract", "[ClassCleanup]")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodsMustBeInTestClassAsync(bool isError, string testClassAttribute, string baseClass, string classQualifier, string testType)
		{
			const string template = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

{0}
public {2} class Tests : {3}
{{
	{1}
	public void Foo() {{ }}
}}";

			await VerifySuccessfulCompilation(string.Format(template, testClassAttribute, testType, classQualifier, baseClass)).ConfigureAwait(false);

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
