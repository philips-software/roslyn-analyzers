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
	public class EnforceBoolNamingConventionAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceBoolNamingConventionAnalyzer();
		}

		[DataRow("_isFoo", true)]
		[DataRow("_areFoo", true)]
		[DataRow("_shouldFoo", true)]
		[DataRow("_hasFoo", true)]
		[DataRow("_doesFoo", true)]
		[DataRow("_wasFoo", true)]
		[DataRow("_is12Foo", true)]
		[DataRow("_foo", false)]
		[DataRow("_isfoo", false)]
		[DataRow("_arefoo", false)]
		[DataRow("_shouldfoo", false)]
		[DataRow("_hasfoo", false)]
		[DataRow("_doesfoo", false)]
		[DataRow("_wasfoo", false)]
		[DataRow("IsFoo", false)]
		[DataRow("AreFoo", false)]
		[DataRow("ShouldFoo", false)]
		[DataRow("HasFoo", false)]
		[DataRow("DoesFoo", false)]
		[DataRow("WasFoo", false)]
		[DataRow("isFoo", false)]
		[DataRow("areFoo", false)]
		[DataRow("shouldFoo", false)]
		[DataRow("hasFoo", false)]
		[DataRow("doesFoo", false)]
		[DataRow("wasFoo", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableNameIsCorrect(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
	private bool {0} = 0;
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceBoolNamingConvention).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task VariableNameIsNotBool()
		{
			var givenText = @"class Foo 
{{
	private int i = 5;
	private void Foo() { int x = 10; }
}}
";

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}


		[DataRow("isfoo", false)]
		[DataRow("arefoo", false)]
		[DataRow("shouldfoo", false)]
		[DataRow("hasfoo", false)]
		[DataRow("doesfoo", false)]
		[DataRow("wasfoo", false)]
		[DataRow("is12foo", false)]
		[DataRow("Isfoo", false)]
		[DataRow("Arefoo", false)]
		[DataRow("Shouldfoo", false)]
		[DataRow("Hasfoo", false)]
		[DataRow("Doesfoo", false)]
		[DataRow("Wasfoo", false)]
		[DataRow("_IsFoo", false)]
		[DataRow("_AreFoo", false)]
		[DataRow("_ShouldFoo", false)]
		[DataRow("_HasFoo", false)]
		[DataRow("_DoesFoo", false)]
		[DataRow("_WasFoo", false)]
		[DataRow("_Foo", false)]
		[DataRow("Foo", false)]
		[DataRow("IsFoo", true)]
		[DataRow("AreFoo", true)]
		[DataRow("ShouldFoo", true)]
		[DataRow("HasFoo", true)]
		[DataRow("DoesFoo", true)]
		[DataRow("WasFoo", true)]
		[DataRow("Is12Foo", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableNameIsCorrectPublic(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
	public bool {0} = 0;
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceBoolNamingConvention).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromConstant()
		{
			var baseline = @"class Foo 
{{
	private const bool IsFoo = true;
}}
";
			var givenText = baseline;
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromConstantValue()
		{
			var baseline = @"class Foo 
{{
	private static readonly bool _isFoo = true;
	private static readonly bool _areFoo = true;
	private static readonly bool _shouldFoo = true;
}}
";
			await VerifySuccessfulCompilation(baseline).ConfigureAwait(false);
		}

		[DataRow("i", false)]
		[DataRow("is", false)]
		[DataRow("are", false)]
		[DataRow("should", false)]
		[DataRow("has", false)]
		[DataRow("does", false)]
		[DataRow("was", false)]
		[DataRow("isA", true)]
		[DataRow("areA", true)]
		[DataRow("shouldA", true)]
		[DataRow("hasA", true)]
		[DataRow("doesA", true)]
		[DataRow("wasA", true)]
		[DataRow("is12", true)]
		[DataRow("isa", false)]
		[DataRow("area", false)]
		[DataRow("shoulda", false)]
		[DataRow("hasa", false)]
		[DataRow("doesa", false)]
		[DataRow("wasa", false)]
		[DataRow("_isFoo", false)]
		[DataRow("__isfoo", false)]
		[DataRow("__isFoo", false)]
		[DataRow("_isfoo", false)]
		[DataRow("_is12foo", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LocalVariableNameIsCorrect(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
	private void Bar()
	{{
		bool {0} = 0;
	}}
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceBoolNamingConvention).ConfigureAwait(false);
			}
		}

		[DataRow("foreach(bool i in new[] { true, false }){}", false)]
		[DataRow("foreach(bool _i in new[] { true, false }){}", false)]
		[DataRow("foreach(bool _I in new[] { true, false }){}", false)]
		[DataRow("foreach(bool _isI in new[] { true, false }){}", false)]
		[DataRow("foreach(bool _areI in new[] { true, false }){}", false)]
		[DataRow("foreach(bool _shouldI in new[] { true, false }){}", false)]
		[DataRow("foreach(bool _hasI in new[] { true, false }){}", false)]
		[DataRow("foreach(bool _doesI in new[] { true, false }){}", false)]
		[DataRow("foreach(bool _wasI in new[] { true, false }){}", false)]
		[DataRow("foreach(bool are in new[] { true, false }){}", false)]
		[DataRow("foreach(bool should in new[] { true, false }){}", false)]
		[DataRow("foreach(bool has in new[] { true, false }){}", false)]
		[DataRow("foreach(bool does in new[] { true, false }){}", false)]
		[DataRow("foreach(bool was in new[] { true, false }){}", false)]
		[DataRow("foreach(bool isf in new[] { true, false }){}", false)]
		[DataRow("foreach(bool aref in new[] { true, false }){}", false)]
		[DataRow("foreach(bool shouldf in new[] { true, false }){}", false)]
		[DataRow("foreach(bool hasf in new[] { true, false }){}", false)]
		[DataRow("foreach(bool doesf in new[] { true, false }){}", false)]
		[DataRow("foreach(bool wasf in new[] { true, false }){}", false)]
		[DataRow("foreach(bool isFoo in new[] { true, false }){}", true)]
		[DataRow("foreach(bool areFoo in new[] { true, false }){}", true)]
		[DataRow("foreach(bool shouldFoo in new[] { true, false }){}", true)]
		[DataRow("foreach(bool hasFoo in new[] { true, false }){}", true)]
		[DataRow("foreach(bool doesFoo in new[] { true, false }){}", true)]
		[DataRow("foreach(bool wasFoo in new[] { true, false }){}", true)]
		[DataRow("foreach(int i in new[] { 55, 22 }){}", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LocalVariableNameIsCorrectForeach(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
	private void Bar()
	{{
		{0}
	}}
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceBoolNamingConvention).ConfigureAwait(false);
			}
		}

		[DataRow("_foo", false)]
		[DataRow("_isFoo", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableNameOfTypeBoolean(string content, bool isGood)
		{
			var baseline = @"using System;
class Foo 
{{
	private Boolean {0} = true;
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceBoolNamingConvention).ConfigureAwait(false);
			}
		}

		[DataRow("foo", false)]
		[DataRow("isFoo", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LocalVariableNameOfTypeBoolean(string content, bool isGood)
		{
			var baseline = @"using System;
class Foo 
{{
	private void Bar()
	{{
		Boolean {0} = true;
	}}
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceBoolNamingConvention).ConfigureAwait(false);
			}
		}

		[DataRow("foo", false)]
		[DataRow("isFoo", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LocalVariableNameOfTypeVar(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
	private void Bar()
	{{
		var {0} = true;
	}}
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceBoolNamingConvention).ConfigureAwait(false);
			}
		}

		[DataRow("Foo", false)]
		[DataRow("foo", false)]
		[DataRow("isFoo", true)]
		[DataRow("areFoo", true)]
		[DataRow("shouldFoo", true)]
		[DataRow("hasFoo", true)]
		[DataRow("doesFoo", true)]
		[DataRow("wasFoo", true)]
		[DataRow("is12Foo", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParameterNameIsCorrect(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
	public void Bar(bool {0})
	{{
        // Some code
	}}
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceBoolNamingConvention).ConfigureAwait(false);
			}
		}

		[DataRow("Foo", false)]
		[DataRow("foo", false)]
		[DataRow("IsFoo", true)]
		[DataRow("AreFoo", true)]
		[DataRow("ShouldFoo", true)]
		[DataRow("HasFoo", true)]
		[DataRow("DoesFoo", true)]
		[DataRow("WasFoo", true)]
		[DataRow("Is12Foo", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PropertyNameIsCorrect(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
	private bool _isFoo = true;
	public bool {0}
	{{
		get {{ return _isFoo; }}
		set {{ _isFoo = value; }}
	}}
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceBoolNamingConvention).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NonBooleanParameterIsIgnored()
		{
			var givenText = @"class Foo 
{{
	public void Bar(int i)
	{{
        // Some code
	}}
}}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NonBooleanPropertyIsIgnored()
		{
			var givenText = @"class Foo 
{{
	public int Index
	{{
		get;
		set;
	}}
}}
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task BaseClassPropertiesAreNotErrors()
		{
			var baseline = @"
using Microsoft.CodeAnalysis.Diagnostics;
class BaseClass 
{
#pragma warning disable PH2060
	public virtual bool CanRead {get;set;}
#pragma warning restore PH2060
}

class Foo : BaseClass 
{
	public override bool CanRead
	{
		get;set;
	}
}
";
			var givenText = baseline;
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task InterfacePropertiesAreNotErrors()
		{
			var baseline = @"
using Microsoft.CodeAnalysis.Diagnostics;
interface BaseClass 
{
#pragma warning disable PH2060
	bool CanRead {get;}
#pragma warning restore PH2060
}

abstract class Foo : BaseClass
{
	public bool CanRead
	{
		set;
	}
}
";
			var givenText = baseline;
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task BaseClassMethodsAreNotErrors()
		{
			var baseline = @"
using System.Windows.Forms;

abstract class Foo : ApplicationContext
{
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}
}
";
			var givenText = baseline;
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedCodeFilesShouldIgnoreField()
		{
			var givenText = @"class Foo 
{{
	public bool Read = true;
}}
";
			await VerifySuccessfulCompilation(givenText, "GlobalSuppressions").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedCodeFilesShouldIgnoreForEach()
		{
			var givenText = @"class Foo 
{{
	public void Method(bool[] bees)
    {{
        foreach (bool read in bees)
        {{
        }}
    }}
}}
";
			await VerifySuccessfulCompilation(givenText, "GlobalSuppressions").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedCodeFilesShouldIgnoreProperty()
		{
			var givenText = @"class Foo 
{{
	public bool Read
	{{
		get;
	}}
}}
";
			await VerifySuccessfulCompilation(givenText, "GlobalSuppressions").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedCodeFilesShouldIgnoreParameter()
		{
			var givenText = @"class Foo 
{{
	public void Bar(bool foo)
	{{
        // Some code
	}}
}}
";
			await VerifySuccessfulCompilation(givenText, "GlobalSuppressions").ConfigureAwait(false);
		}

	}
}
