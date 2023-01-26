// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class DontLockNewObjectAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members
		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DontLockNewObjectAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataRow("this", false)]
		[DataRow("lockObj", false)]
		[DataRow("new object()", true)]
		[DataRow("new object().ToString()", true)]
		[DataTestMethod]
		public void PreventLockOnUncapturedVariable(string lockText, bool expectedError)
		{
			string text = @$"
public class Foo
{{
	public void TestMethod()
	{{
		object lockObj = new object();
		lock({lockText}){{}}
	}}
}}
";

			VerifyDiagnostic(text, expectedError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.DontLockNewObject) : Array.Empty<DiagnosticResult>());
		}

		#endregion
	}
}
