// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class NoProtectedFieldsAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		#endregion

		#region Public Interface

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NoProtectedFieldsAnalyzer();
		}

		[DataRow("protected", true)]
		[DataRow("public", false)]
		[DataRow("private", false)]
		[DataRow("private", false)]
		[DataRow("internal", false)]
		[DataTestMethod]
		public void ProtectedFieldsRaiseError(string modifiers, bool isError)
		{
			const string template = @"""
class Foo {{ {0} string _foo; }}
""";

			DiagnosticResult[] expected = isError ? new[] { DiagnosticResultHelper.Create(DiagnosticIds.NoProtectedFields) } : Array.Empty<DiagnosticResult>();

			VerifyDiagnostic(string.Format(template, modifiers), expected);
		}

		#endregion
	}
}
