// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	/// <summary>
	/// Test class for <see cref="AlignFilenameAndClassNameAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AlignFilenameAndClassNameAnalyzerTest : DiagnosticVerifier
	{
		private const string SourceCodeTemplate = @"
namespace AlignFilenameAndClassName {{
    {0} Program {{
    }}
}}";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("class", "Program"),
		 DataRow("class", "Program.Part"),
		 DataRow("struct", "Program"),
		 DataRow("enum", "Program.Part.Of.Many.Things")]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string typeKind, string filePath)
		{
			VerifyDiagnostic(string.Format(SourceCodeTemplate, typeKind), filePath);
		}

		/// <summary>
		/// Diagnostics should show up hare.
		/// </summary>
		[DataTestMethod]
		[DataRow("class", "Program2"),
		 DataRow("struct", "SomethingElse"),
		 DataRow("enum", "Prog")]
		public void WhenNamesDontAlignDiagnosticIsRaised(string typeKind, string filePath)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.AlignFilenameAndClassName);
			VerifyDiagnostic(string.Format(SourceCodeTemplate, typeKind), filePath, expected);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AlignFilenameAndClassNameAnalyzer();
		}
	}
}
