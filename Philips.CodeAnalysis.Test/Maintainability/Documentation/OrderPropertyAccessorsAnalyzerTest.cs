using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class OrderPropertyAccessorsAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members
		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new OrderPropertyAccessorsAnalyzer();
		}

		#endregion

		#region Public Interface
		[DataRow(@"{ get; set; }", false)]
		[DataRow(@"{ get; }", false)]
		[DataRow(@"{ get { return null; } }", false)]
		[DataRow(@"{ set {} }", false)]
		[DataRow(@"{ get; init; }", false)]
		[DataRow(@"{ init; get; }", true)]
		[DataRow(@"{ set; get; }", true)]
		[DataRow(@"{ set{ } get{ return default; } }", true)]
		[DataTestMethod]
		public void OrderTests(string property, bool isError)
		{
			string text = $@"
public class TestClass
{{
	public string Foo {property}
}}
";

			VerifyCSharpDiagnostic(text, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.OrderPropertyAccessors) : Array.Empty<DiagnosticResult>());
		}

		#endregion
	}
}
