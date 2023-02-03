// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestMethodsMustBeInTestClassAnalyzer();
		}

		protected override (string name, string content)[] GetAdditionalSourceCode()
		{
			string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class DerivedTestMethod : TestMethod
{
}

";

			return new[] { ("DerivedTestMethod.cs", code) };
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
		public void TestMethodsMustBeInTestClass(bool isError, string baseClass, string classQualifier, string testType)
		{
			const string template = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

{0}
public {2} class Tests : {3}
{{
	{1}
	public void Foo() {{ }}
}}";

			VerifySuccessfulCompilation(string.Format(template, "[TestClass]", testType, classQualifier, baseClass));

			var code = string.Format(template, "", testType, classQualifier, baseClass);
			if (isError)
			{
				var expectedResult = new[] { DiagnosticResultHelper.Create(DiagnosticId.TestMethodsMustBeInTestClass) };
				VerifyDiagnostic(code, expectedResult);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}
		#endregion
	}
}
