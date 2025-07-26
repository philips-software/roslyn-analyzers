// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidVariablesNamedUnderscoreAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidVariablesNamedUnderscoreAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldNamedUnderscoreShouldTriggerDiagnostic()
		{
			const string template = @"
class Foo
{
	private string _ = ""test"";
}
";
			await VerifyDiagnostic(template, DiagnosticId.AvoidVariablesNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LocalVariableNamedUnderscoreShouldTriggerDiagnostic()
		{
			const string template = @"
class Foo
{
	private void Bar()
	{
		string _ = ""test"";
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.AvoidVariablesNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ForEachVariableNamedUnderscoreShouldTriggerDiagnostic()
		{
			const string template = @"
class Foo
{
	private void Bar()
	{
		foreach (var _ in new[] { 1, 2, 3 })
		{
		}
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.AvoidVariablesNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ForLoopVariableNamedUnderscoreShouldTriggerDiagnostic()
		{
			const string template = @"
class Foo
{
	private void Bar()
	{
		for (var _ = 0; _ < 10; _++)
		{
		}
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.AvoidVariablesNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UsingVariableNamedUnderscoreShouldTriggerDiagnostic()
		{
			const string template = @"
using System.IO;

class Foo
{
	private void Bar()
	{
		using (var _ = new MemoryStream())
		{
		}
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.AvoidVariablesNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValidFieldNameShouldNotTriggerDiagnostic()
		{
			const string template = @"
class Foo
{
	private string _validField = ""test"";
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValidLocalVariableNameShouldNotTriggerDiagnostic()
		{
			const string template = @"
class Foo
{
	private void Bar()
	{
		string validVariable = ""test"";
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValidForEachVariableNameShouldNotTriggerDiagnostic()
		{
			const string template = @"
class Foo
{
	private void Bar()
	{
		foreach (var item in new[] { 1, 2, 3 })
		{
		}
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}
	}
}