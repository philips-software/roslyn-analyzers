// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class RemoveCommentedCodeAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new RemoveCommentedCodeAnalyzer();
		}

		private const string WrongCommentedLine = @"
			// int i = 0;
			int j = 1;
			";

		[DataTestMethod]
		[DataRow(@"")]
		[DataRow(@"// Some comment")]
		[DataRow(@"// Some comment ending with a dot.")]
		[DataRow(@"// For example: int i = 0.")]
		public void TextualCommentAreFine(string content)
		{
			VerifySuccessfulCompilation(content);
		}

		[DataTestMethod]
		[DataRow(WrongCommentedLine, DisplayName = nameof(WrongCommentedLine))]
		public void CodeCommentsShouldTriggerDiagnostic(string content)
		{
			DiagnosticResult expected = DiagnosticResultHelper.Create(DiagnosticIds.RemoveCommentedCode);

			VerifyDiagnostic(content, expected);
		}
	}
}
