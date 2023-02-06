// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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

		private const string WrongCommentedChunk = @"
			// int i = 0;
            // int k = 9;
			int j = 1;
			";

		[DataTestMethod]
		[DataRow(@"")]
		[DataRow(@"// Some comment")]
		[DataRow(@"// Some comment ending with a dot.")]
		[DataRow(@"// For example: int i = 0.")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void TextualCommentAreFine(string content)
		{
			VerifySuccessfulCompilation(content);
		}

		[DataTestMethod]
		[DataRow(WrongCommentedLine, DisplayName = nameof(WrongCommentedLine)),
		 DataRow(WrongCommentedChunk, DisplayName = nameof(WrongCommentedChunk))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CodeCommentsShouldTriggerDiagnostic(string content)
		{
			VerifyDiagnostic(content);
		}
	}
}
