// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
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
		#region Non-Public Data Members

		#endregion

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
		public void FieldVariableNameIsCorrect(string content, bool isGood)
		{
			string baseline = @"class Foo 
{{
    private int {0} = 0;
}}
";
			string givenText = string.Format(baseline, content);

			if (isGood)
			{
				VerifySuccessfulCompilation(givenText);
			}
			else
			{
				VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions);
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
		public void FieldVariableNameIgnoresPublicFields(string content, bool isGood)
		{
			string baseline = @"class Foo 
{{
    public int {0} = 0;
}}
";
			string givenText = string.Format(baseline, content);

			if (isGood)
			{
				VerifySuccessfulCompilation(givenText);
			}
			else
			{
				VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromArray()
		{
			string baseline = @"class Foo 
{{
    private static readonly char BigAsterisk = char.ConvertFromUtf32(0x2731)[0];
}}
";
			string givenText = baseline;
			VerifySuccessfulCompilation(givenText);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromConstant()
		{
			string baseline = @"class Foo 
{{
    private const uint ProcessorArchitectureValid = 0x001;
}}
";
			string givenText = baseline;

			VerifySuccessfulCompilation(givenText);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromCastConstant()
		{
			string baseline = @"class Foo 
{{
    private readonly System.IntPtr InvalidHandle = (System.IntPtr)(-1);
}}
";
			string givenText = baseline;

			VerifySuccessfulCompilation(givenText);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromCharArray()
		{
			string baseline = @"class Foo 
{{
    private readonly char[] KeyPathDelimiters = new char[] { '\\' };
}}
";
			string givenText = baseline;

			VerifySuccessfulCompilation(givenText);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromImplicitArray()
		{
			string baseline = @"public static class Foo 
{{
    private static readonly string[] AntiVirusBasePaths = new[] { @""SOFTWARE"", @""SOFTWARE\Wow6432Node"" };
}}
";
			string givenText = baseline;

			VerifySuccessfulCompilation(givenText);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromImplicitArrayInitializer()
		{
			string baseline = @"public static class Foo 
{{
    private static readonly string[] AntiVirusBasePaths = { @""SOFTWARE"", @""SOFTWARE\Wow6432Node"" };
}}
";
			string givenText = baseline;

			VerifySuccessfulCompilation(givenText);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromArrayWrittenTo()
		{
			string baseline = @"public static class Foo 
{{
    private static readonly bool[] _starIdentifiers = new bool[128];

    private static void Set(int index)
    {
        _starIdentifiers[index] = true;
    }
}}
";
			string givenText = baseline;

			VerifySuccessfulCompilation(givenText);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromFunction()
		{
			string baseline = @"public static class Foo 
{{
    private static readonly Icon DeviceLocationOutOfBounds = Get<Icon>(""DeviceLocationOutOfBounds"");
}
	}
";
			string givenText = baseline;

			VerifySuccessfulCompilation(givenText);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromDateTime()
		{
			string baseline = @"public static class Foo 
{{
    private static readonly System.DateTime UnixTimeZero = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
}}
";
			string givenText = baseline;

			VerifySuccessfulCompilation(givenText);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableFromConstantValue()
		{
			string baseline = @"class Foo 
{{
    private static readonly System.IntPtr CurrentServerHandle = System.IntPtr.Zero;
}}
";
			string givenText = baseline;

			VerifySuccessfulCompilation(givenText);
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
		public void EventNameIsCorrect(string content, bool isGood)
		{
			string baseline = @"class Foo 
{{
    public event EventHandler {0} = null;
}}
";
			string givenText = string.Format(baseline, content);
			if (isGood)
			{
				VerifySuccessfulCompilation(givenText);
			}
			else
			{
				VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions);
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
		public void LocalVariableNameIsCorrect(string content, bool isGood)
		{
			string baseline = @"class Foo 
{{
    private void Bar()
    {{
        int {0} = 0;
    }}
}}
";
			string givenText = string.Format(baseline, content);

			if (isGood)
			{
				VerifySuccessfulCompilation(givenText);
			}
			else
			{
				VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions);
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
		public void AttributedLocalVariableNameIsCorrect(string content, string attribute, bool isGood)
		{
			string baseline = @"class Foo 
{{
    private void Bar()
    {{
        {1} int {0} = 0;
    }}
}}
";
			string givenText = string.Format(baseline, content, attribute);
			if (isGood)
			{
				VerifySuccessfulCompilation(givenText);
			}
			else
			{
				VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions);
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
		public void LocalVariableNameIsCorrectForLoop(string content, bool isGood)
		{
			string baseline = @"class Foo 
{{
    private void Bar()
    {{
        {0}
    }}
}}
";
			string givenText = string.Format(baseline, content);
			if (isGood)
			{
				VerifySuccessfulCompilation(givenText);
			}
			else
			{
				VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions);
			}
		}

		[DataRow("using(var i = new MemoryStream()){}", true)]
		[DataRow("using(var _i = new MemoryStream()){}", false)]
		[DataRow("using(var _I = new MemoryStream()){}", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LocalVariableNameIsCorrectUsing(string content, bool isGood)
		{
			string baseline = @"class Foo 
{{
    private void Bar()
    {{
        {0}
    }}
}}
";
			string givenText = string.Format(baseline, content);
			if (isGood)
			{
				VerifySuccessfulCompilation(givenText);
			}
			else
			{
				VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions);
			}
		}

		[DataRow("foreach(var i in new[] { 1, 2 }){}", true)]
		[DataRow("foreach(var _i in new[] { 1, 2 }){}", false)]
		[DataRow("foreach(var _I in new[] { 1, 2 }){}", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LocalVariableNameIsCorrectForeach(string content, bool isGood)
		{
			string baseline = @"class Foo 
{{
    private void Bar()
    {{
        {0}
    }}
}}
";
			string givenText = string.Format(baseline, content);
			if (isGood)
			{
				VerifySuccessfulCompilation(givenText);
			}
			else
			{
				VerifyDiagnostic(givenText, DiagnosticId.VariableNamingConventions);
			}
		}

		#endregion
	}
}
