// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class PositiveNamingAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PositiveNamingAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NegativeField()
		{
			const string template = @"
using System;
class Foo
{
	private bool ignoreWhitespace;
}
";

			await VerifyDiagnostic(template, DiagnosticId.PositiveNaming).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PositiveField()
		{
			const string template = @"
using System;
class Foo
{
	private bool enableFeature;
}
";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NegativeLocalVariable()
		{
			const string template = @"
using System;
class Foo
{
	public void Test(int a)
	{
		bool disableFeature;
	}
}
";

			await VerifyDiagnostic(template, DiagnosticId.PositiveNaming).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NegativeProperty()
		{
			const string template = @"
using System;
class Foo
{
	public bool featureMissing { get; }
}
";

			await VerifyDiagnostic(template, DiagnosticId.PositiveNaming).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NegativeFieldInTestClass()
		{
			const string template = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo
{
	private bool ignoreWhitespace;
}
";

			await VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NegativeLocalVariableInTestClassShouldBeIgnored()
		{
			const string template = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo
{
    [TestMethod]
	public void Test()
	{
		bool disableFeature;
	}
}
";

			await VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NegativePropertyInTestClassShouldBeIgnored()
		{
			const string template = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo
{
	public bool featureMissing { get; }
}
";

			await VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NagativeLocalVariablesInGeneratedCodeFilesShouldBeIgnored()
		{
			var givenText = @"class Foo 
{{
    public bool featureMissing { get; }
}}
";
			await VerifySuccessfulCompilation(givenText, "GlobalSuppressions").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NegativePropertiesInGeneratedCodeFilesShouldBeIgnored()
		{
			var givenText = @"class Foo 
{{
    public void Bar()
    {{
        bool disableFeature;
    }}
}}
";
			await VerifySuccessfulCompilation(givenText, "GlobalSuppressions").ConfigureAwait(false);
		}
	}
}
