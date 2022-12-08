// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="LogExceptionAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class LogExceptionAnalyzerTest : DiagnosticVerifier
	{
		private const string configuredLogMethods = "TestLog,TestTrace";
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

    private static void LogDebug(string message) {
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

    private static void Debug(string message) {
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

    private static void Debug(string message) {
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
		[TestMethod]
		[DataRow(Correct, DisplayName = "Correct"),
			DataRow(CorrectThrow, DisplayName = "CorrectThrow"),
			DataRow(CorrectVerboseTracer, DisplayName = "CorrectVerboseTracer")]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(Missing, DisplayName = "Missing")]
		public void WhenExceptionIsNotLoggedDiagnosticIsTriggered(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.LogException); 
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow(Missing, "Dummy.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyCSharpDiagnostic(testCode, filePath);
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
			Dictionary<string, string> options = new()
			{
				{ $@"dotnet_code_quality.{ Helper.ToDiagnosticId(DiagnosticIds.LogException) }.log_method_names", configuredLogMethods }
			};
			return options;
		}
	}
}
