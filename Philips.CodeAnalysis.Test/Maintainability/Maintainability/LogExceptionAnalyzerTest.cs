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
	/// Test class for <see cref="LogExceptionAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class LogExceptionAnalyzerTest : DiagnosticVerifier
	{
		private const string ConfiguredLogMethods = @"
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
            Logger.TestLog('Goodbye');            
        }
    }

    private class Logger {
        public static void TestLog(string message) {
        }
    }
}
}";

		private const string CorrectLogClass = @"
using System;

namespace LogExceptionUnitTests {
public class Program {
    public static void Main(string[] args) {
        try {
            Console.WriteLine('Hello world!');
        } catch {
            Log.SomeLog('Goodbye');            
        }
    }

    private class Log {
        public static void SomeLog(string message) {
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

		private const string CorrectILoggerError = @"
using System;

namespace LogExceptionUnitTests {
public class MyService {
    private readonly ILogger _logger;

    public MyService(ILogger logger) {
        _logger = logger;
    }

    public void TestMethod() {
        try {
            Console.WriteLine('Hello world!');
        } catch (Exception ex) {
            _logger.LogError(ex, ""Error while processing subscription from {0}"", ""destination"");
        }
    }
}

public interface ILogger {
    void LogError(Exception ex, string message, string destination);
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
		[DataRow(Correct, DisplayName = nameof(Correct)),
		 DataRow(CorrectLogClass, DisplayName = nameof(CorrectLogClass)),
		 DataRow(CorrectThrow, DisplayName = nameof(CorrectThrow)),
		 DataRow(CorrectVerboseTracer, DisplayName = nameof(CorrectVerboseTracer)),
		 DataRow(CorrectILoggerError, DisplayName = nameof(CorrectILoggerError))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Missing, DisplayName = nameof(Missing))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenExceptionIsNotLoggedDiagnosticIsTriggered(string testCode)
		{
			await VerifyDiagnostic(testCode, DiagnosticId.LogException).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(Missing, "Dummy.g", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			await VerifySuccessfulCompilation(testCode, filePath).ConfigureAwait(false);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LogExceptionAnalyzer();
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return base.GetAdditionalTexts().Add((LogExceptionAnalyzer.AllowedFileName, ConfiguredLogMethods));
		}
	}
}
