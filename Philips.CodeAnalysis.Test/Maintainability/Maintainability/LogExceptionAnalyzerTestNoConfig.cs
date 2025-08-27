// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	/// Test class for <see cref="LogExceptionAnalyzer"/>, when not configured at all.
	/// </summary>
	[TestClass]
	public class LogExceptionAnalyzerTestNoConfig : DiagnosticVerifier
	{
		private const string CorrectCode = @"
using System;

namespace LogExceptionUnitTests {
public class Program {
    public static void Main(string[] args) {
        try {
            Console.WriteLine('Hello world!');
        } catch {
            Log.LogVerbose('Goodbye');            
        }
    }

    private class Log {
        public static void LogVerbose(string message) {
        }
    }
}
}";

		private const string WrongCode = @"
using System;

namespace LogExceptionUnitTests {
public class Program {
    public static void Main(string[] args) {
        try {
            Console.WriteLine('Hello world!');
        } catch {
        }
    }
}
}";

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(CorrectCode, DisplayName = nameof(CorrectCode))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenCallingLogClassNoDiagnosticShouldBeTriggered(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(WrongCode, DisplayName = nameof(WrongCode))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenExceptionIsNotLoggedDiagnosticShouldBeTriggered(string testCode)
		{
			await VerifyDiagnostic(testCode, DiagnosticId.LogException).ConfigureAwait(false);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LogExceptionAnalyzer();
		}
	}
}
