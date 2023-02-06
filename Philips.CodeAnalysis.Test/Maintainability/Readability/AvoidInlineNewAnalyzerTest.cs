﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.


using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	/// <summary>
	/// 
	/// </summary>
	[TestClass]
	public class AvoidInlineNewAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
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


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorOnAllowedMethods()
		{
			var file = CreateFunction("string str = new object().ToString()");
			VerifySuccessfulCompilation(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontInlineNewCall()
		{
			var file = CreateFunction("int hash = new object().GetHashCode()");
			Verify(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorIfPlacedInLocal()
		{
			var file = CreateFunction("object obj = new object(); string str = obj.ToString();");
			VerifySuccessfulCompilation(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorIfPlacedInField()
		{
			var file = CreateFunction("_obj = new object(); string str = _obj.ToString();");
			VerifySuccessfulCompilation(file);
		}

		[DataRow("new Foo()")]
		[DataRow("(new Foo())")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DontInlineNewCallCustomType(string newVariant)
		{
			var file = CreateFunction($"int hash = {newVariant}.GetHashCode()");
			Verify(file);
		}

		[DataRow("new Foo()")]
		[DataRow("(new Foo())")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorInlineNewCallCustomTypeAllowedMethod(string newVariant)
		{
			var file = CreateFunction($"string str = {newVariant}.ToString()");
			VerifySuccessfulCompilation(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorIfPlacedInLocalCustomType()
		{
			var file = CreateFunction("object obj = new Foo(); string str = obj.ToString();");
			VerifySuccessfulCompilation(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorIfPlacedInFieldCustomType()
		{
			var file = CreateFunction("_obj = new Foo(); string str = _obj.ToString();");
			VerifySuccessfulCompilation(file);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorIfPlacedInContainer()
		{
			var file = CreateFunction("var v = new List<object>(); v.Add(new object());");
			VerifySuccessfulCompilation(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorIfReturned()
		{
			var file = CreateFunction("return new object();");
			VerifySuccessfulCompilation(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ErrorIfReturned()
		{
			var file = CreateFunction("return new object().GetHashCode();");
			Verify(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorIfReturnedAllowedMethod()
		{
			var file = CreateFunction("return new object().ToString();");
			VerifySuccessfulCompilation(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorIfThrown()
		{
			var file = CreateFunction("throw new Exception();");
			VerifySuccessfulCompilation(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ErrorIfThrown()
		{
			var file = CreateFunction("throw new object().Foo;");
			Verify(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoErrorOnAsSpanMethod()
		{
			var file = CreateFunction("new string(\"\").AsSpan();");
			VerifySuccessfulCompilation(file);
		}

		private void Verify(string file)
		{
			VerifyDiagnostic(file);
		}
	}
}
