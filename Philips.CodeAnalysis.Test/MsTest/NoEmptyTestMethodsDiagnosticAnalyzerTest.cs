// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
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
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NoEmptyTestMethodsDiagnosticAnalyzer();
		}

		protected override MetadataReference[] GetMetadataReferences()
		{
			return new[] { MetadataReference.CreateFromFile(typeof(TimeoutAttribute).Assembly.Location) };
		}
		protected override (string name, string content)[] GetAdditionalSourceCode()
		{
			string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class DerivedTestMethod : TestMethodAttribute
{
}

";

			return new[] { ("DerivedTestMethod.cs", code) };
		}

		#endregion

		#region Public Interface

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
		public void EmptyMethodTriggersAnalyzer(string attribute)
		{
			const string template = @"using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass] public class Foo {{ [{0}] public void Method() {{ }} }}";

			VerifyDiagnostic(string.Format(template, attribute), DiagnosticResultHelper.Create(DiagnosticIds.TestMethodsMustNotBeEmpty));
		}

		#endregion
	}
}
