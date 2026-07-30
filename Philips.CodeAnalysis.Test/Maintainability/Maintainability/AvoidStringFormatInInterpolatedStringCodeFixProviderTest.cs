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

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixMultipleArgumentsWithTextTest()
		{
			var givenText = @"
using System;

class Test
{
	public void Method()
	{
		var name = ""John"";
		var age = 25;
		var result = $""User: {string.Format(""Name is {0}, Age is {1:D2}"", name, age)}"";
	}
}
";

			var expectedText = @"
using System;

class Test
{
	public void Method()
	{
		var name = ""John"";
		var age = 25;
		var result = $""User: Name is {name}, Age is {age:D2}"";
	}
}
";

			await VerifyDiagnostic(givenText, DiagnosticId.AvoidStringFormatInInterpolatedString).ConfigureAwait(false);
			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixEmptyFormatStringTest()
		{
			var givenText = @"
using System;

class Test
{
	public void Method()
	{
		var result = $""Value: {string.Format("""")}"";
	}
}
";

			var expectedText = @"
using System;

class Test
{
	public void Method()
	{
		var result = $""Value: "";
	}
}
";

			await VerifyDiagnostic(givenText, DiagnosticId.AvoidStringFormatInInterpolatedString).ConfigureAwait(false);
			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixNoPlaceholdersTest()
		{
			var givenText = @"
using System;

class Test
{
	public void Method()
	{
		var value = 42;
		var result = $""Value: {string.Format(""static text"", value)}"";
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
		var result = $""Value: static text"";
	}
}
";

			await VerifyDiagnostic(givenText, DiagnosticId.AvoidStringFormatInInterpolatedString).ConfigureAwait(false);
			await VerifyFix(givenText, expectedText, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixWithMixedTextAndPlaceholdersTest()
		{
			var givenText = @"
using System;

class Test
{
	public void Method()
	{
		var name = ""John"";
		var age = 25;
		var status = ""active"";
		var result = $""Info: {string.Format(""User {0} is {1:D2} years old and {2}"", name, age, status)}"";
	}
}
";

			var expectedText = @"
using System;

class Test
{
	public void Method()
	{
		var name = ""John"";
		var age = 25;
		var status = ""active"";
		var result = $""Info: User {name} is {age:D2} years old and {status}"";
	}
}
";

			await VerifyDiagnostic(givenText, DiagnosticId.AvoidStringFormatInInterpolatedString).ConfigureAwait(false);
			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixWithComplexFormatSpecifiersTest()
		{
			var givenText = @"
using System;

class Test
{
	public void Method()
	{
		var price = 123.45m;
		var count = 7;
		var result = $""Order: {string.Format(""{0:C} x {1:D3}"", price, count)}"";
	}
}
";

			var expectedText = @"
using System;

class Test
{
	public void Method()
	{
		var price = 123.45m;
		var count = 7;
		var result = $""Order: {price:C} x {count:D3}"";
	}
}
";

			await VerifyDiagnostic(givenText, DiagnosticId.AvoidStringFormatInInterpolatedString).ConfigureAwait(false);
			await VerifyFix(givenText, expectedText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixWithOnlyTextTest()
		{
			var givenText = @"
using System;

class Test
{
	public void Method()
	{
		var value = 42;
		var result = $""Value: {string.Format(""constant text"", value)}"";
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
		var result = $""Value: constant text"";
	}
}
";

			await VerifyDiagnostic(givenText, DiagnosticId.AvoidStringFormatInInterpolatedString).ConfigureAwait(false);
			await VerifyFix(givenText, expectedText, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixWithTrailingTextTest()
		{
			var givenText = @"
using System;

class Test
{
	public void Method()
	{
		var name = ""John"";
		var result = $""Hello {string.Format(""{0} Doe"", name)}"";
	}
}
";

			var expectedText = @"
using System;

class Test
{
	public void Method()
	{
		var name = ""John"";
		var result = $""Hello {name} Doe"";
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
