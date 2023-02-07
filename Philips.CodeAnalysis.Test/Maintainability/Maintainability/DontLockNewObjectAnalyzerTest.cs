// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
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
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DontLockNewObjectAnalyzer();
		}

	
		[DataRow("this", false)]
		[DataRow("lockObj", false)]
		[DataRow("new object()", true)]
		[DataRow("new object().ToString()", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task PreventLockOnUncapturedVariableAsync(string lockText, bool isExpectedError)
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

			if (isExpectedError)
			{
				await VerifyDiagnostic(text, DiagnosticId.DontLockNewObject).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(text).ConfigureAwait(false);
			}
		}

	}
}
