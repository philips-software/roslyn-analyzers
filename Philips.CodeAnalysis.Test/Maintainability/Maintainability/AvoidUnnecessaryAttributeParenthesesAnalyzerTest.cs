// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidUnnecessaryAttributeParenthesesAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidUnnecessaryAttributeParenthesesAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidUnnecessaryAttributeParenthesesCodeFixProvider();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenAttributeHasArgumentsDoNotTrigger()
		{
			const string template = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{{
	[TestClass]
	public class UnitTest1
	{{
		[DataRow(""test"")]
		public void TestMethod1()
		{{
		}}
	}}
}}";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenAttributeHasNoParenthesesDoNotTrigger()
		{
			const string template = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{{
	[TestClass]
	public class UnitTest1
	{{
		[TestMethod]
		public void TestMethod1()
		{{
		}}
	}}
}}";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenAttributeHasEmptyParenthesesTrigger()
		{
			const string template = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{{
	[TestClass()]
	public class UnitTest1
	{{
		[TestMethod]
		public void TestMethod1()
		{{
		}}
	}}
}}";

			await VerifyDiagnostic(template, DiagnosticId.AvoidUnnecessaryAttributeParentheses).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidUnnecessaryAttributeParenthesesCodeFixProviderTest()
		{
			const string template = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{{
	[TestClass()]
	public class UnitTest1
	{{
		[TestMethod()]
		public void TestMethod1()
		{{
		}}
	}}
}}";

			const string expected = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{{
	[TestClass]
	public class UnitTest1
	{{
		[TestMethod]
		public void TestMethod1()
		{{
		}}
	}}
}}";

			await VerifyFix(template, expected).ConfigureAwait(false);
		}
	}
}
