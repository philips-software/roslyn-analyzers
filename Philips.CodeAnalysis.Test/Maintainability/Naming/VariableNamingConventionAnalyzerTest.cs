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

		[DataRow("foo", false, 3)]
		[DataRow("_foo", true, 3)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableNameIsCorrect(string content, bool isGood, int errorLine)
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
				var expected = 
					new DiagnosticResult
					{
						Id = Helper.ToDiagnosticId(DiagnosticId.VariableNamingConventions),
						Message = new Regex(".+"),
						Severity = DiagnosticSeverity.Error,
						Locations = new[]
						{
							new DiagnosticResultLocation("Test.cs", errorLine, 13)
						}
					};
				VerifyDiagnostic(givenText, expected);
			}
		}

		[DataRow("foo", true, 3)]
		[DataRow("Foo", true, 3)]
		[DataRow("Foo_", true, 3)]
		[DataRow("_Foo", true, 3)]
		[DataRow("__foo", true, 3)]
		[DataRow("__Foo", true, 3)]
		[DataRow("_foo", true, 3)]
		[DataRow("_foo_", true, 3)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void FieldVariableNameIgnoresPublicFields(string content, bool isGood, int errorLine)
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
				var expected = new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.VariableNamingConventions),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test.cs", errorLine, 13)
					}
				};
				VerifyDiagnostic(givenText, expected);
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

		[DataRow("foo", false, 3)]
		[DataRow("Foo", true, 3)]
		[DataRow("_Foo", false, 3)]
		[DataRow("__foo", false, 3)]
		[DataRow("__Foo", false, 3)]
		[DataRow("_foo", false, 3)]
		[DataRow("_foo_", false, 3)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EventNameIsCorrect(string content, bool isGood, int errorLine)
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
				var expected = new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.VariableNamingConventions),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test.cs", errorLine, 18)
					}
				};
				VerifyDiagnostic(givenText, expected);
			}
		}

		[DataRow("i", true, 5)]
		[DataRow("ms", true, 5)]
		[DataRow("foo", true, 5)]
		[DataRow("_Foo", false, 5)]
		[DataRow("__foo", false, 5)]
		[DataRow("__Foo", false, 5)]
		[DataRow("_foo", false, 5)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LocalVariableNameIsCorrect(string content, bool isGood, int errorLine)
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
				var expected = new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.VariableNamingConventions),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test.cs", errorLine, 9)
					}
				};
				VerifyDiagnostic(givenText, expected);
			}
		}

		[DataRow("Foo", "const", true, 5)]
		[DataRow("Foo", "const", true, 5)]
		[DataRow("_Foo", "const", false, 5)]
		[DataRow("__foo", "const", false, 5)]
		[DataRow("__Foo", "const", false, 5)]
		[DataRow("_foo", "const", false, 5)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AttributedLocalVariableNameIsCorrect(string content, string attribute, bool isGood, int errorLine)
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
				var expected = new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.VariableNamingConventions),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test.cs", errorLine, 8 + attribute.Length + 2)
					}
				};
				VerifyDiagnostic(givenText, expected);
			}
		}

		[DataRow("int i; for(i=0;i<5;i++){}", true, 5, 9)]
		[DataRow("int _i; for(_i=0;i<5;i++){}", false, 5, 9)]
		[DataRow("int _i; for(_i=0;i<5;i++){}", false, 5, 9)]
		[DataRow("for(var i=0;i<5;i++){}", true, 5, 13)]
		[DataRow("for(var _i=0;i<5;i++){}", false, 5, 13)]
		[DataRow("for(var _i=0;i<5;i++){}", false, 5, 13)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LocalVariableNameIsCorrectForLoop(string content, bool isGood, int errorLine, int errorColumn)
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
				var expected = new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.VariableNamingConventions),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test.cs", errorLine, errorColumn)
					}
				};
				VerifyDiagnostic(givenText, expected);
			}
		}

		[DataRow("using(var i = new MemoryStream()){}", true, 5)]
		[DataRow("using(var _i = new MemoryStream()){}", false, 5)]
		[DataRow("using(var _I = new MemoryStream()){}", false, 5)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LocalVariableNameIsCorrectUsing(string content, bool isGood, int errorLine)
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
				var expected = new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.VariableNamingConventions),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test.cs", errorLine, 15)
					}
				};
				VerifyDiagnostic(givenText, expected);
			}
		}

		[DataRow("foreach(var i in new[] { 1, 2 }){}", true, 5)]
		[DataRow("foreach(var _i in new[] { 1, 2 }){}", false, 5)]
		[DataRow("foreach(var _I in new[] { 1, 2 }){}", false, 5)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LocalVariableNameIsCorrectForeach(string content, bool isGood, int errorLine)
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
				var expected = new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.VariableNamingConventions),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test.cs", errorLine, 9)
					}
				};
				VerifyDiagnostic(givenText, expected);
			}
		}

		#endregion
	}
}
