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
			VerifyDiagnostic(CreateFunction("string str = new object().ToString()"));
		}

		[TestMethod]
		public void NoErrorIfPlacedInLocal()
		{
			VerifyNoDiagnostic(CreateFunction("object obj = new object(); string str = obj.ToString();"));
		}

		[TestMethod]
		public void NoErrorIfPlacedInField()
		{
			VerifyNoDiagnostic(CreateFunction("_obj = new object(); string str = _obj.ToString();"));
		}

		[DataRow("new Foo()")]
		[DataRow("(new Foo())")]
		[DataTestMethod]
		public void DontInlineNewCallCustomType(string newVarient)
		{
			VerifyDiagnostic(CreateFunction($"string str = {newVarient}.ToString()"));
		}

		[TestMethod]
		public void NoErrorIfPlacedInLocalCustomType()
		{
			VerifyNoDiagnostic(CreateFunction("object obj = new Foo(); string str = obj.ToString();"));
		}

		[TestMethod]
		public void NoErrorIfPlacedInFieldCustomType()
		{
			VerifyNoDiagnostic(CreateFunction("_obj = new Foo(); string str = _obj.ToString();"));
		}


		[TestMethod]
		public void NoErrorIfPlacedInContainer()
		{
			VerifyNoDiagnostic(CreateFunction("var v = new List<object>(); v.Add(new object());"));
		}

		[TestMethod]
		public void NoErrorIfReturned()
		{
			VerifyNoDiagnostic(CreateFunction("return new object();"));
		}

		[TestMethod]
		public void ErrorIfReturned()
		{
			VerifyDiagnostic(CreateFunction("return new object().ToString();"));
		}

		[TestMethod]
		public void NoErrorIfThrown()
		{
			VerifyNoDiagnostic(CreateFunction("throw new Exception();"));
		}

		[TestMethod]
		public void ErrorIfThrown()
		{
			VerifyDiagnostic(CreateFunction("throw new object().Foo;"));
		}

		private void VerifyNoDiagnostic(string file)
		{
			VerifyCSharpDiagnostic(file, new DiagnosticResult[0]);
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
