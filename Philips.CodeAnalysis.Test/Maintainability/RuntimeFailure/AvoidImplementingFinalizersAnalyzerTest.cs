// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="AvoidImplementingFinalizersAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidImplementingFinalizersAnalyzerTest : DiagnosticVerifier
	{
		private const string Correct =
@"// Copyright Koninklijke Philips N.V. 2020

using System;

namespace PathTooLongUnitTest {
    class Program {
        ~Program() {
        }
    }
}";
		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		public void WhenTestCodeHasFinalizerDiagnosticIsTriggered()
		{
			VerifyDiagnostic(Correct, DiagnosticResultHelper.Create(DiagnosticIds.AvoidImplementingFinalizers));
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
			return new AvoidImplementingFinalizersAnalyzer();
		}
	}
}
