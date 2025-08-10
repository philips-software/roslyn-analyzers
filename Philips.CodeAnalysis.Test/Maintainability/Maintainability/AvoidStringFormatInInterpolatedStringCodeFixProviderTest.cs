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
	public class AvoidStringFormatInInterpolatedStringCodeFixProviderTest : CodeFixVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixSimpleStringFormatTest()
		{
			var givenText = @"
using System;

class Test
{
	public void Method()
	{
		var firstName = ""John"";
		var lastName = ""Doe"";
		var result = $""Hello {string.Format(""{0} {1}"", firstName, lastName)}"";
	}
}
";

			var expectedText = @"
using System;

class Test
{
	public void Method()
	{
		var firstName = ""John"";
		var lastName = ""Doe"";
		var result = $""Hello {firstName} {lastName}"";
	}
}
";

			await VerifyDiagnostic(givenText, DiagnosticId.AvoidStringFormatInInterpolatedString).ConfigureAwait(false);
			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixStringFormatWithFormatSpecifierTest()
		{
			var givenText = @"
using System;

class Test
{
	public void Method()
	{
		var value = 42;
		var result = $""Value: {string.Format(""{0:D2}"", value)}"";
	}
}
";

			var expectedText = @"
using System;

class Test
{
	public void Method()
	{
		var value = 42;
		var result = $""Value: {value:D2}"";
	}
}
";

			await VerifyDiagnostic(givenText, DiagnosticId.AvoidStringFormatInInterpolatedString).ConfigureAwait(false);
			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidStringFormatInInterpolatedStringCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidStringFormatInInterpolatedStringAnalyzer();
		}
	}
}