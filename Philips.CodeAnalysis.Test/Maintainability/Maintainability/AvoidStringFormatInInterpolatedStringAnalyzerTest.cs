// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidStringFormatInInterpolatedStringAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidStringFormatInInterpolatedStringAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task StringFormatInInterpolatedStringTriggersWarning()
		{
			var code = @"
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
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task StringFormatWithMultipleArgumentsTriggersWarning()
		{
			var code = @"
using System;

class Test
{
	public void Method()
	{
		var name = ""John"";
		var age = 30;
		var result = $""User info: {string.Format(""Name: {0}, Age: {1}"", name, age)}"";
	}
}
";
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task InterpolatedStringWithoutStringFormatIsOk()
		{
			var code = @"
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
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task StringFormatOutsideInterpolatedStringIsOk()
		{
			var code = @"
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
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}
	}
}
