// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="NoSpaceInFilenameAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class NoSpaceInFilenameAnalyzerTest : DiagnosticVerifier
	{
		private const string Correct =
@"// Copyright Koninklijke Philips N.V. 2020

using System;

namespace PathTooLongUnitTest {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine('Hello world!');
        }
    }
}";
		private const string CorrectName = "Correct.cs";
		private const string SpaceName = "Incorrect Name.cs";
		private const string SpaceAbsolutePath = "C:\\My Documents\\Wrong.cs";
		private const string SpaceRelativePath = "..\\..\\My Code\\Wrong.cs";
		private const string OutOfScopePath = @"./GlobalSuppressions.cs";

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(CorrectName, DisplayName = "CorrectName"),
		 DataRow(OutOfScopePath, DisplayName = "OutOfScopePath")]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string filePath)
		{
			VerifyCSharpDiagnostic(Correct, filePath);
		}

		/// <summary>
		/// Diagnostic is expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(SpaceName, 1, 1, DisplayName = "SpaceName"),
		 DataRow(SpaceAbsolutePath, 1, 1, DisplayName = "SpaceAbsolutePath"),
		 DataRow(SpaceRelativePath, 1, 1, DisplayName = "SpaceRelativePath")]
		public void WhenFileNameHasSpaceDiagnosticIsRaised(string filePath, int line, int column)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.NoSpaceInFilename);
			VerifyCSharpDiagnostic(Correct, filePath, expected);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new NoSpaceInFilenameAnalyzer();
		}
	}
}
