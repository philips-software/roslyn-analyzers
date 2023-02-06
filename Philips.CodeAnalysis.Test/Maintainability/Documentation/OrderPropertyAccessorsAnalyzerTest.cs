// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class OrderPropertyAccessorsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new OrderPropertyAccessorsAnalyzer();
		}

		[DataRow(@"{ get; set; }")]
		[DataRow(@"{ get; }")]
		[DataRow(@"{ get { return null; } }")]
		[DataRow(@"{ set {} }")]
		[DataRow(@"{ get; init; }")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void OrderValidTests(string property)
		{
			string text = $@"
public class TestClass
{{
	public string Foo {property}
}}
";

			VerifySuccessfulCompilation(text);
		}

		[DataRow(@"{ init; get; }")]
		[DataRow(@"{ set; get; }")]
		[DataRow(@"{ set{ } get{ return default; } }")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void OrderInvalidTests(string property)
		{
			string text = $@"
public class TestClass
{{
	public string Foo {property}
}}
";

			VerifyDiagnostic(text);
		}
	}
}
