// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="LogExceptionAnalyzer"/>, for using .editorconfig to configure.
	/// </summary>
	[TestClass]
	public class LogExceptionAnalyzerTestFromEditorConfig : DiagnosticVerifier
	{
		private const string ConfiguredLogMethods = "EditorTestTrace,EditorTestLog";

		private const string CorrectCode = @"
using System;

namespace LogExceptionUnitTests {
public class Program {
    public static void Main(string[] args) {
        try {
            Console.WriteLine('Hello world!');
        } catch {
            Logger.EditorTestLog('Goodbye');            
        }
    }

    private class Logger {
        public static void EditorTestLog(string message) {
        }
    }
}
}";
		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(CorrectCode, DisplayName = "CorrectCode")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenExceptionIsLoggedNoDiagnosticShouldBeTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LogExceptionAnalyzer();
		}

		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			return base.GetAdditionalAnalyzerConfigOptions().Add($@"dotnet_code_quality.{DiagnosticId.LogException.ToId()}.log_method_names", ConfiguredLogMethods);
		}
	}
}
