// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="LogExceptionAnalyzer"/>, for wrong .editorconfig usage.
	/// </summary>
	[TestClass]
	public class LogExceptionAnalyzerTests : DiagnosticVerifier
	{
		private const string configuredLogMethods = "TestLog,TestTrace";
		private const string CorrectCode = @"
using System;

namespace LogExceptionUnitTests {
public class Program {
    public static void Main(string[] args) {
        try {
            Console.WriteLine('Hello world!');
        } catch {
            Log.TestLog('Goodbye');            
        }
    }

    private static void LogDebug(string message) {
    }
}
}";
		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(CorrectCode, DisplayName = "CorrectCode")]
		public void WhenExceptionIsNotLoggedDiagnosticIsTriggered(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.LogException);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new LogExceptionAnalyzer();
		}

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ $@"dotnet_code_quality.{ Helper.ToDiagnosticId(DiagnosticIds.LogException) }.wrong_method_names", configuredLogMethods }
			};
			return options;
		}
	}
}
