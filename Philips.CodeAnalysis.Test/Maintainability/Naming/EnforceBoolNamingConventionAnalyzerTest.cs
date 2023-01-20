// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.


using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class EnforceBoolNamingConventionAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceBoolNamingConventionAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataRow("_isFoo", true, 3)]
		[DataRow("_areFoo", true, 3)]
		[DataRow("_shouldFoo", true, 3)]
		[DataRow("_hasFoo", true, 3)]
		[DataRow("_doesFoo", true, 3)]
		[DataRow("_wasFoo", true, 3)]
		[DataRow("_is12Foo", true, 3)]
		[DataRow("_foo", false, 3)]
		[DataRow("_isfoo", false, 3)]
		[DataRow("_arefoo", false, 3)]
		[DataRow("_shouldfoo", false, 3)]
		[DataRow("_hasfoo", false, 3)]
		[DataRow("_doesfoo", false, 3)]
		[DataRow("_wasfoo", false, 3)]
		[DataRow("IsFoo", false, 3)]
		[DataRow("AreFoo", false, 3)]
		[DataRow("ShouldFoo", false, 3)]
		[DataRow("HasFoo", false, 3)]
		[DataRow("DoesFoo", false, 3)]
		[DataRow("WasFoo", false, 3)]
		[DataRow("isFoo", false, 3)]
		[DataRow("areFoo", false, 3)]
		[DataRow("shouldFoo", false, 3)]
		[DataRow("hasFoo", false, 3)]
		[DataRow("doesFoo", false, 3)]
		[DataRow("wasFoo", false, 3)]
		[DataTestMethod]
		public void FieldVariableNameIsCorrect(string content, bool isGood, int errorLine)
		{
			string baseline = @"class Foo 
{{
	private bool {0} = 0;
}}
";
			string givenText = string.Format(baseline, content);

			DiagnosticResult[] expected;

			if (isGood)
			{
				expected = Array.Empty<DiagnosticResult>();
			}
			else
			{
				expected = new[] { new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceBoolNamingConvention),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test.cs", errorLine, 15)
				} }
				};
			}

			VerifyDiagnostic(givenText, expected);
		}

		[DataRow("isfoo", false, 3)]
		[DataRow("arefoo", false, 3)]
		[DataRow("shouldfoo", false, 3)]
		[DataRow("hasfoo", false, 3)]
		[DataRow("doesfoo", false, 3)]
		[DataRow("wasfoo", false, 3)]
		[DataRow("is12foo", false, 3)]
		[DataRow("Isfoo", false, 3)]
		[DataRow("Arefoo", false, 3)]
		[DataRow("Shouldfoo", false, 3)]
		[DataRow("Hasfoo", false, 3)]
		[DataRow("Doesfoo", false, 3)]
		[DataRow("Wasfoo", false, 3)]
		[DataRow("_IsFoo", false, 3)]
		[DataRow("_AreFoo", false, 3)]
		[DataRow("_ShouldFoo", false, 3)]
		[DataRow("_HasFoo", false, 3)]
		[DataRow("_DoesFoo", false, 3)]
		[DataRow("_WasFoo", false, 3)]
		[DataRow("_Foo", false, 3)]
		[DataRow("Foo", false, 3)]
		[DataRow("IsFoo", true, 3)]
		[DataRow("AreFoo", true, 3)]
		[DataRow("ShouldFoo", true, 3)]
		[DataRow("HasFoo", true, 3)]
		[DataRow("DoesFoo", true, 3)]
		[DataRow("WasFoo", true, 3)]
		[DataRow("Is12Foo", true, 3)]
		[DataTestMethod]
		public void FieldVariableNameIsCorrectPublic(string content, bool isGood, int errorLine)
		{
			string baseline = @"class Foo 
{{
	public bool {0} = 0;
}}
";
			string givenText = string.Format(baseline, content);

			DiagnosticResult[] expected;

			if (isGood)
			{
				expected = Array.Empty<DiagnosticResult>();
			}
			else
			{
				expected = new[] { new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceBoolNamingConvention),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test.cs", errorLine, 14)
				} }
				};
			}

			VerifyDiagnostic(givenText, expected);
		}

		[TestMethod]
		public void FieldVariableFromConstant()
		{
			string baseline = @"class Foo 
{{
	private const bool IsFoo = true;
}}
";
			string givenText = baseline;

			DiagnosticResult[] expected = Array.Empty<DiagnosticResult>();

			VerifyDiagnostic(givenText, expected);
		}

		[TestMethod]
		public void FieldVariableFromConstantValue()
		{
			string baseline = @"class Foo 
{{
	private static readonly bool _isFoo = true;
	private static readonly bool _areFoo = true;
	private static readonly bool _shouldFoo = true;
}}
";
			string givenText = baseline;

			DiagnosticResult[] expected = Array.Empty<DiagnosticResult>();

			VerifyDiagnostic(givenText, expected);
		}

		[DataRow("i", false, 5)]
		[DataRow("is", false, 5)]
		[DataRow("are", false, 5)]
		[DataRow("should", false, 5)]
		[DataRow("has", false, 5)]
		[DataRow("does", false, 5)]
		[DataRow("was", false, 5)]
		[DataRow("isA", true, 5)]
		[DataRow("areA", true, 5)]
		[DataRow("shouldA", true, 5)]
		[DataRow("hasA", true, 5)]
		[DataRow("doesA", true, 5)]
		[DataRow("wasA", true, 5)]
		[DataRow("is12", true, 5)]
		[DataRow("isa", false, 5)]
		[DataRow("area", false, 5)]
		[DataRow("shoulda", false, 5)]
		[DataRow("hasa", false, 5)]
		[DataRow("doesa", false, 5)]
		[DataRow("wasa", false, 5)]
		[DataRow("_isFoo", false, 5)]
		[DataRow("__isfoo", false, 5)]
		[DataRow("__isFoo", false, 5)]
		[DataRow("_isfoo", false, 5)]
		[DataRow("_is12foo", false, 5)]
		[DataTestMethod]
		public void LocalVariableNameIsCorrect(string content, bool isGood, int errorLine)
		{
			string baseline = @"class Foo 
{{
	private void Bar()
	{{
		bool {0} = 0;
	}}
}}
";
			string givenText = string.Format(baseline, content);

			DiagnosticResult[] expected;

			if (isGood)
			{
				expected = Array.Empty<DiagnosticResult>();
			}
			else
			{
				expected = new[] { new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceBoolNamingConvention),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test.cs", errorLine, 8)
				} }
				};
			}

			VerifyDiagnostic(givenText, expected);
		}

		[DataRow("foreach(bool i in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool _i in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool _I in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool _isI in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool _areI in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool _shouldI in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool _hasI in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool _doesI in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool _wasI in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool are in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool should in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool has in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool does in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool was in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool isf in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool aref in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool shouldf in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool hasf in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool doesf in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool wasf in new[] { true, false }){}", false, 5)]
		[DataRow("foreach(bool isFoo in new[] { true, false }){}", true, 5)]
		[DataRow("foreach(bool areFoo in new[] { true, false }){}", true, 5)]
		[DataRow("foreach(bool shouldFoo in new[] { true, false }){}", true, 5)]
		[DataRow("foreach(bool hasFoo in new[] { true, false }){}", true, 5)]
		[DataRow("foreach(bool doesFoo in new[] { true, false }){}", true, 5)]
		[DataRow("foreach(bool wasFoo in new[] { true, false }){}", true, 5)]
		[DataTestMethod]
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

			DiagnosticResult[] expected;

			if (isGood)
			{
				expected = Array.Empty<DiagnosticResult>();
			}
			else
			{
				expected = new[] { new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceBoolNamingConvention),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test.cs", errorLine, 16)
				} }
				};
			}

			VerifyDiagnostic(givenText, expected);
		}

		[DataRow("_foo", false, 4)]
		[DataRow("_isFoo", true, 4)]
		[DataTestMethod]
		public void FieldVariableNameOfTypeBoolean(string content, bool isGood, int errorLine)
		{
			string baseline = @"using System;
class Foo 
{{
	private Boolean {0} = true;
}}
";
			string givenText = string.Format(baseline, content);

			DiagnosticResult[] expected;

			if (isGood)
			{
				expected = Array.Empty<DiagnosticResult>();
			}
			else
			{
				expected = new[] { new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceBoolNamingConvention),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test.cs", errorLine, 18)
				} }
				};
			}

			VerifyDiagnostic(givenText, expected);
		}

		[DataRow("foo", false, 6)]
		[DataRow("isFoo", true, 6)]
		[DataTestMethod]
		public void LocalVariableNameOfTypeBoolean(string content, bool isGood, int errorLine)
		{
			string baseline = @"using System;
class Foo 
{{
	private void Bar()
	{{
		Boolean {0} = true;
	}}
}}
";
			string givenText = string.Format(baseline, content);

			DiagnosticResult[] expected;

			if (isGood)
			{
				expected = Array.Empty<DiagnosticResult>();
			}
			else
			{
				expected = new[] { new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceBoolNamingConvention),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test.cs", errorLine, 11)
				} }
				};
			}

			VerifyDiagnostic(givenText, expected);
		}

		[DataRow("foo", false, 5)]
		[DataRow("isFoo", true, 5)]
		[DataTestMethod]
		public void LocalVariableNameOfTypeVar(string content, bool isGood, int errorLine)
		{
			string baseline = @"class Foo 
{{
	private void Bar()
	{{
		var {0} = true;
	}}
}}
";
			string givenText = string.Format(baseline, content);

			DiagnosticResult[] expected;

			if (isGood)
			{
				expected = Array.Empty<DiagnosticResult>();
			}
			else
			{
				expected = new[] { new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceBoolNamingConvention),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test.cs", errorLine, 7)
				} }
				};
			}

			VerifyDiagnostic(givenText, expected);
		}

		[DataRow("Foo", false, 4)]
		[DataRow("foo", false, 4)]
		[DataRow("IsFoo", true, 4)]
		[DataRow("AreFoo", true, 4)]
		[DataRow("ShouldFoo", true, 4)]
		[DataRow("HasFoo", true, 4)]
		[DataRow("DoesFoo", true, 4)]
		[DataRow("WasFoo", true, 4)]
		[DataRow("Is12Foo", true, 4)]
		[DataTestMethod]
		public void PropertyNameIsCorrect(string content, bool isGood, int errorLine)
		{
			string baseline = @"class Foo 
{{
	private bool _isFoo = true;
	public bool {0}
	{{
		get {{ return _isFoo; }}
		set {{ _isFoo = value; }}
	}}
}}
";
			string givenText = string.Format(baseline, content);

			DiagnosticResult[] expected;

			if (isGood)
			{
				expected = Array.Empty<DiagnosticResult>();
			}
			else
			{
				expected = new[] { new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceBoolNamingConvention),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test.cs", errorLine, 14)
				} }
				};
			}

			VerifyDiagnostic(givenText, expected);
		}


		[TestMethod]
		public void BaseClassPropertiesAreNotErrors()
		{
			string baseline = @"
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
			string givenText = baseline;

			DiagnosticResult[] expected = Array.Empty<DiagnosticResult>();

			VerifyDiagnostic(givenText, expected);
		}

		[TestMethod]
		public void InterfacePropertiesAreNotErrors()
		{
			string baseline = @"
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
			string givenText = baseline;

			DiagnosticResult[] expected = Array.Empty<DiagnosticResult>();

			VerifyDiagnostic(givenText, expected);
		}

		[TestMethod]
		public void BaseClassMethodsAreNotErrors()
		{
			string baseline = @"
using System.Windows.Forms;

abstract class Foo : ApplicationContext
{
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}
}
";
			string givenText = baseline;

			DiagnosticResult[] expected = Array.Empty<DiagnosticResult>();

			VerifyDiagnostic(givenText, expected);
		}

		#endregion
	}
}
