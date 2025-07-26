// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class PreferInterpolatedStringAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PreferInterpolatedStringAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DetectStringFormatWithSingleArgument()
		{
			const string input = @"
class Foo
{
	public void Test()
	{
		int num = 42;
		string str = string.Format(""This is number {0}"", num);
	}
}";

			await VerifyDiagnostic(input, DiagnosticId.PreferInterpolatedString).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DetectUnnecessaryStringFormatWithoutPlaceholders()
		{
			const string input = @"
class Foo
{
	public void Test()
	{
		string str = string.Format(""Simple string"");
	}
}";

			await VerifyDiagnostic(input, DiagnosticId.PreferInterpolatedString).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoWarningForStringWithFormatSpecifiers()
		{
			const string input = @"
class Foo
{
	public void Test()
	{
		decimal value = 123.456m;
		string str = string.Format(""Value: {0:N2}"", value);
	}
}";

			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoWarningForNonLiteralFormatString()
		{
			const string input = @"
class Foo
{
	public void Test()
	{
		string format = ""Hello {0}"";
		string name = ""World"";
		string str = string.Format(format, name);
	}
}";

			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoWarningForEscapedBraces()
		{
			const string input = @"
class Foo
{
	public void Test()
	{
		string name = ""World"";
		string str = string.Format(""Hello {{static text}} {0}"", name);
	}
}";

			await VerifyDiagnostic(input, DiagnosticId.PreferInterpolatedString).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NoWarningForOnlyEscapedBraces()
		{
			const string input = @"
class Foo
{
	public void Test()
	{
		string str = string.Format(""Hello {{static text}} no placeholders"");
	}
}";

			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DetectUnnecessaryStringFormat()
		{
			const string input = @"
class Foo
{
	public void Test()
	{
		string str = string.Format(""Simple string with no placeholders"");
	}
}";

			await VerifyDiagnostic(input, DiagnosticId.PreferInterpolatedString).ConfigureAwait(false);
		}
	}
}
