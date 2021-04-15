// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="LimitPathLengthAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class LimitPathLengthAnalyzerTest : DiagnosticVerifier
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

		private const string ShortName = "Short.cs";
		private const string ShortAbsolutePath = @"C:\Short.cs";
		private const string ShortRelativePath = "../Short.cs";
		private const string LongName =
			"VeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVery" +
			"VeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVery" +
			"VeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVery" +
			"VeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryLong";
		private const string LongAbsolutePath =
			"C:\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\" +
			"Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\" +
			"Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\" +
			"Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Long";
		private const string LongRelativePath =
			".\\.\\.\\.\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\" +
			"Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\" +
			"Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\" +
			"Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Long";
		private const string GeneratedFilePath =
			"C:\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\" +
			"Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\" +
			"Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\" +
			"Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Very\\Long.g";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(ShortName, DisplayName = "ShortName"),
		 DataRow(ShortAbsolutePath, DisplayName = "ShortAbsolutePath"),
		 DataRow(ShortRelativePath, DisplayName = "ShortRelativePath")]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string filePath)
		{
			VerifyCSharpDiagnostic(Correct, filePath);
		}

		/// <summary>
		/// Diagnostics should show up hare.
		/// </summary>
		[TestMethod]
		[DataRow(LongName, DisplayName = "LongName"),
		 DataRow(LongAbsolutePath, DisplayName = "LongAbsolutePath"),
		 DataRow(LongRelativePath, DisplayName = "LongRelativePath"),
		 DataRow(GeneratedFilePath, DisplayName = "GeneratedFile")]
		public void WhenPathIsTooLongDiagnosticIsRaised(string filePath) {
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.LimitPathLength);
			VerifyCSharpDiagnostic(Correct, filePath, expected);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new LimitPathLengthAnalyzer();
		}
	}
}
