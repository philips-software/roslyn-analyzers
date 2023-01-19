// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.


using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	/// <summary>
	/// 
	/// </summary>
	[TestClass]
	public class AvoidInlineNewAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidInlineNewAnalyzer();
		}

		private string CreateFunction(string content)
		{
			string baseline = @"
class Foo 
{{
  public void Foo()
  {{
    {0};
  }}
}}
";

			return string.Format(baseline, content);
		}

		#endregion

		#region Public Interface

		[TestMethod]
		public void DontInlineNewCall()
		{
			var file = CreateFunction("string str = new object().ToString()");
			VerifyDiagnostic(file);
		}

		[TestMethod]
		public void NoErrorIfPlacedInLocal()
		{
			var file = CreateFunction("object obj = new object(); string str = obj.ToString();");
			VerifyNoDiagnostic(file);
		}

		[TestMethod]
		public void NoErrorIfPlacedInField()
		{
			var file = CreateFunction("_obj = new object(); string str = _obj.ToString();");
			VerifyNoDiagnostic(file);
		}

		[DataRow("new Foo()")]
		[DataRow("(new Foo())")]
		[DataTestMethod]
		public void DontInlineNewCallCustomType(string newVarient)
		{
			var file = CreateFunction($"string str = {newVarient}.ToString()");
			VerifyDiagnostic(file);
		}

		[TestMethod]
		public void NoErrorIfPlacedInLocalCustomType()
		{
			var file = CreateFunction("object obj = new Foo(); string str = obj.ToString();");
			VerifyNoDiagnostic(file);
		}

		[TestMethod]
		public void NoErrorIfPlacedInFieldCustomType()
		{
			var file = CreateFunction("_obj = new Foo(); string str = _obj.ToString();");
			VerifyNoDiagnostic(file);
		}


		[TestMethod]
		public void NoErrorIfPlacedInContainer()
		{
			var file = CreateFunction("var v = new List<object>(); v.Add(new object());");
			VerifyNoDiagnostic(file);
		}

		[TestMethod]
		public void NoErrorIfReturned()
		{
			var file = CreateFunction("return new object();");
			VerifyNoDiagnostic(file);
		}

		[TestMethod]
		public void ErrorIfReturned()
		{
			var file = CreateFunction("return new object().ToString();");
			VerifyDiagnostic(file);
		}

		[TestMethod]
		public void NoErrorIfThrown()
		{
			var file = CreateFunction("throw new Exception();");
			VerifyNoDiagnostic(file);
		}

		[TestMethod]
		public void ErrorIfThrown()
		{
			var file = CreateFunction("throw new object().Foo;");
			VerifyDiagnostic(file);
		}

		private void VerifyNoDiagnostic(string file)
		{
			VerifyCSharpDiagnostic(file);
		}

		private void VerifyDiagnostic(string file)
		{
			VerifyCSharpDiagnostic(file, new DiagnosticResult()
			{
				Id = AvoidInlineNewAnalyzer.Rule.Id,
				Message = new Regex(".+"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 6, -1),
				}
			});
		}

		#endregion
	}
}
