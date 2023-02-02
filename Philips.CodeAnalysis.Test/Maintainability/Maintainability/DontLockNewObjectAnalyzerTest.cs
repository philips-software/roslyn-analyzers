﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;
using static System.Runtime.InteropServices.JavaScript.JSType;


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
		[TestCategory(TestDefinitions.UnitTests)]
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

			if (expectedError)
			{
				VerifyDiagnostic(text, DiagnosticResultHelper.Create(DiagnosticIds.DontLockNewObject));
			}
			else
			{
				VerifySuccessfulCompilation(text);
			}
		}

		#endregion
	}
}
