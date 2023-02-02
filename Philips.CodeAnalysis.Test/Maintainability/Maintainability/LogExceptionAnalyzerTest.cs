// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="LogExceptionAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class LogExceptionAnalyzerTest : DiagnosticVerifier
	{
		private const string configuredLogMethods = @"
*.*.TestLog
TestTrace
";

		private const string Correct = @"
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

    private class Log {
        public static void TestLog(string message) {
        }
    }
}
}";

		private const string CorrectThrow = @"
using System;

namespace LogExceptionUnitTests {
public class Program {
    public static void Main(string[] args) {
        try {
            Console.WriteLine('Hello world!');
        } catch (Exception ex) {
            throw new AggregateException('message', ex);
        }
    }
}";

		private const string CorrectVerboseTracer = @"
using System;

namespace LogExceptionUnitTests {
public class Program {
    public static void Main(string[] args) {
        try {
            Console.WriteLine('Hello world!');
        } catch (Exception ex) {
            Tracer.TestTrace('message');
        }
    }

    private class Tracer {
        public static void TestTrace(string message) {
        }
    }
}
}";

		private const string Missing = @"
using System;

namespace LogExceptionUnitTests {
public class Program {
    public static void Main(string[] args) {
        try {
            Console.WriteLine('Hello world!');
        } catch (Exception ex) {
            TraceDebug(ex.Message);           
        }
    }

    private static void TraceDebug(string message) {
    }
}
}";

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Correct, DisplayName = "Correct"),
			DataRow(CorrectThrow, DisplayName = "CorrectThrow"),
			DataRow(CorrectVerboseTracer, DisplayName = "CorrectVerboseTracer")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Missing, DisplayName = "Missing")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenExceptionIsNotLoggedDiagnosticIsTriggered(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticId.LogException); 
			VerifyDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(Missing, "Dummy.g", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyDiagnostic(testCode, filePath);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LogExceptionAnalyzer();
		}

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new [] {(LogExceptionAnalyzer.AllowedFileName, configuredLogMethods)};
		}
	}
}
