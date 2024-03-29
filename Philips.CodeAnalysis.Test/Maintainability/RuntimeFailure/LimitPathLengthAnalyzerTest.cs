﻿// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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
		[DataTestMethod]
		[DataRow(ShortName, DisplayName = "ShortName"),
		 DataRow(ShortAbsolutePath, DisplayName = "ShortAbsolutePath"),
		 DataRow(ShortRelativePath, DisplayName = "ShortRelativePath")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string filePath)
		{
			await VerifySuccessfulCompilation(Correct, filePath).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics should show up hare.
		/// </summary>
		[DataTestMethod]
		[DataRow(LongName, DisplayName = "LongName"),
		 DataRow(LongAbsolutePath, DisplayName = "LongAbsolutePath"),
		 DataRow(LongRelativePath, DisplayName = "LongRelativePath"),
		 DataRow(GeneratedFilePath, DisplayName = "GeneratedFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenPathIsTooLongDiagnosticIsRaisedAsync(string filePath)
		{
			await VerifyDiagnostic(Correct, DiagnosticId.LimitPathLength, filePath).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LimitPathLengthAnalyzer();
		}
	}
}
