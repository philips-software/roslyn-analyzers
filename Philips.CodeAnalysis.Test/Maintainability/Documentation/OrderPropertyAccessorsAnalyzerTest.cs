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
		#region Non-Public Data Members
		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
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

			VerifyDiagnostic(text, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.OrderPropertyAccessors) : Array.Empty<DiagnosticResult>());
		}

		#endregion
	}
}
