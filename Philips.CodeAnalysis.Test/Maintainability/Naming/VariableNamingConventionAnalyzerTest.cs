// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class VariableNamingConventionAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new VariableNamingConventionAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataRow("foo", false)]
		[DataRow("_foo", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableNameIsCorrect(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
    private int {0} = 0;
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions).ConfigureAwait(false);
			}
		}

		[DataRow("foo", true)]
		[DataRow("Foo", true)]
		[DataRow("Foo_", true)]
		[DataRow("_Foo", true)]
		[DataRow("__foo", true)]
		[DataRow("__Foo", true)]
		[DataRow("_foo", true)]
		[DataRow("_foo_", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableNameIgnoresPublicFields(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
    public int {0} = 0;
}}
";
			var givenText = string.Format(baseline, content);

			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromArray()
		{
			var baseline = @"class Foo 
{{
    private static readonly char BigAsterisk = char.ConvertFromUtf32(0x2731)[0];
}}
";
			var givenText = baseline;
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromConstant()
		{
			var baseline = @"class Foo 
{{
    private const uint ProcessorArchitectureValid = 0x001;
}}
";
			var givenText = baseline;

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromCastConstant()
		{
			var baseline = @"class Foo 
{{
    private readonly System.IntPtr InvalidHandle = (System.IntPtr)(-1);
}}
";
			var givenText = baseline;

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromCharArray()
		{
			var baseline = @"class Foo 
{{
    private readonly char[] KeyPathDelimiters = new char[] { '\\' };
}}
";
			var givenText = baseline;

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromImplicitArray()
		{
			var baseline = @"public static class Foo 
{{
    private static readonly string[] AntiVirusBasePaths = new[] { @""SOFTWARE"", @""SOFTWARE\Wow6432Node"" };
}}
";
			var givenText = baseline;

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromImplicitArrayInitializer()
		{
			var baseline = @"public static class Foo 
{{
    private static readonly string[] AntiVirusBasePaths = { @""SOFTWARE"", @""SOFTWARE\Wow6432Node"" };
}}
";
			var givenText = baseline;

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromArrayWrittenTo()
		{
			var baseline = @"public static class Foo 
{{
    private static readonly bool[] _starIdentifiers = new bool[128];

    private static void Set(int index)
    {
        _starIdentifiers[index] = true;
    }
}}
";
			var givenText = baseline;

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromFunction()
		{
			var baseline = @"public static class Foo 
{{
    private static readonly Icon DeviceLocationOutOfBounds = Get<Icon>(""DeviceLocationOutOfBounds"");
}
	}
";
			var givenText = baseline;

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableFromDateTime()
		{
			var baseline = @"public static class Foo 
{{
    private static readonly System.DateTime UnixTimeZero = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
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
    private static readonly System.IntPtr CurrentServerHandle = System.IntPtr.Zero;
}}
";
			var givenText = baseline;

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[DataRow("foo", false)]
		[DataRow("Foo", true)]
		[DataRow("_Foo", false)]
		[DataRow("__foo", false)]
		[DataRow("__Foo", false)]
		[DataRow("_foo", false)]
		[DataRow("_foo_", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EventNameIsCorrect(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
    public event EventHandler {0} = null;
}}
";
			var givenText = string.Format(baseline, content);
			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions).ConfigureAwait(false);
			}
		}

		[DataRow("i", true)]
		[DataRow("ms", true)]
		[DataRow("foo", true)]
		[DataRow("_Foo", false)]
		[DataRow("__foo", false)]
		[DataRow("__Foo", false)]
		[DataRow("_foo", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LocalVariableNameIsCorrect(string content, bool isGood)
		{
			var baseline = @"class Foo 
{{
    private void Bar()
    {{
        int {0} = 0;
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
				await VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions).ConfigureAwait(false);
			}
		}

		[DataRow("Foo", "const", true)]
		[DataRow("Foo", "const", true)]
		[DataRow("_Foo", "const", false)]
		[DataRow("__foo", "const", false)]
		[DataRow("__Foo", "const", false)]
		[DataRow("_foo", "const", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AttributedLocalVariableNameIsCorrect(string content, string attribute, bool isGood)
		{
			var baseline = @"class Foo 
{{
    private void Bar()
    {{
        {1} int {0} = 0;
    }}
}}
";
			var givenText = string.Format(baseline, content, attribute);
			if (isGood)
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions).ConfigureAwait(false);
			}
		}

		[DataRow("int i; for(i=0;i<5;i++){}", true)]
		[DataRow("int _i; for(_i=0;i<5;i++){}", false)]
		[DataRow("int _i; for(_i=0;i<5;i++){}", false)]
		[DataRow("for(var i=0;i<5;i++){}", true)]
		[DataRow("for(var _i=0;i<5;i++){}", false)]
		[DataRow("for(var _i=0;i<5;i++){}", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LocalVariableNameIsCorrectForLoop(string content, bool isGood)
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
				await VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions).ConfigureAwait(false);
			}
		}

		[DataRow("using(var i = new MemoryStream()){}", true)]
		[DataRow("using(var _i = new MemoryStream()){}", false)]
		[DataRow("using(var _I = new MemoryStream()){}", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LocalVariableNameIsCorrectUsing(string content, bool isGood)
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
				await VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions).ConfigureAwait(false);
			}
		}

		[DataRow("foreach(var i in new[] { 1, 2 }){}", true)]
		[DataRow("foreach(var _i in new[] { 1, 2 }){}", false)]
		[DataRow("foreach(var _I in new[] { 1, 2 }){}", false)]
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
				await VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions).ConfigureAwait(false);
			}
		}

		#endregion
	}
}
