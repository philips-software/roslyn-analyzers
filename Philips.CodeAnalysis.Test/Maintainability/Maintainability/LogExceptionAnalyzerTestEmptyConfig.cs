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
	/// Test class for <see cref="LogExceptionAnalyzer"/>, when the AdditionalFile is empty.
	/// </summary>
	[TestClass]
	public class LogExceptionAnalyzerTestEmptyConfig : DiagnosticVerifier
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
            Logging.LogVerbose('Goodbye');            
        }
    }

    private class Log {
        public static void LogVerbose(string message) {
        }
    }
}
}";

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LogClassShouldStillBeRecognized()
		{
			await VerifySuccessfulCompilation(CorrectCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenCallingWrongClassDiagnosticShouldBeTriggered()
		{
			await VerifyDiagnostic(WrongCode, DiagnosticId.LogException).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LogExceptionAnalyzer();
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return base.GetAdditionalTexts().Add((LogExceptionAnalyzer.AllowedFileName, string.Empty));
		}
	}
}
