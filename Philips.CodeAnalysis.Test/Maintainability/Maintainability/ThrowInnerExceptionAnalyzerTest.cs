// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="ThrowInnerExceptionAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class ThrowInnerExceptionAnalyzerTest : DiagnosticVerifier
	{
		private const string Correct = @"
using System;

namespace InnerExceptionUnitTest {
    public class Program {
        public static void Main(string[] args) {
            try {
                Console.WriteLine('Hello world!');
            } catch (Exception ex) {
                throw new AggregateException('message', ex);
            }
        }
    }
}";

		private const string NoRethrow = @"
using System;

namespace InnerExceptionUnitTest {
    public class Log {
        public void LogDebug(string message) {
        }
    }
    public class Program {
        public static void Main(string[] args) {
            try {
                Console.WriteLine('Hello world!');
            } catch (Exception ex) {
                Log.LogDebug(ex.Message);
            }
        }
    }
}";

		private const string RethrowOriginal = @"
using System;

namespace InnerExceptionUnitTest {
    public class Program {
        public static void Main(string[] args) {
            try {
                Console.WriteLine('Hello world!');
            } catch (Exception ex) {
                throw ex;
            }
        }
    }
}";

		private const string HttpResponseInline = @"

using System;

namespace InnerExceptionUnitTest {
        public class Program {
            public static void Main(string[] args) {
                try {

            } catch (SocketException ex) {
                throw new HttpResponseException(
                    HttpRequestException.CreateErrorResponse(HttpStatusCode.BadRequest, 'Some message', ex)
                );
            }
        }
}";

		private const string HttpResponseSeparate = @"

using System;

namespace InnerExceptionUnitTest {
        public class Program {
            public static void Main(string[] args) {
                try {

            } catch (SocketException ex) {
                var response = HttpRequestException.CreateErrorResponse(HttpStatusCode.BadRequest, 'Some message', ex);
                throw new HttpResponseException(response);
            }
        }
}";

		private const string ThrowOther = @"
using System;

namespace InnerExceptionUnitTest {
    public class Program {
        public static void Main(string[] args) {
            try {
                Console.WriteLine('Hello world!');
            } catch (Exception ex) {
                throw new Exception(ex.Message);
            }
        }
    }
}";

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Correct, DisplayName = "Correct"),
			DataRow(NoRethrow, DisplayName = "NoRethrow"),
			DataRow(RethrowOriginal, DisplayName = "RethrowOriginal"),
			DataRow(HttpResponseInline, DisplayName = "HttpResponseInline"),
			DataRow(HttpResponseSeparate, DisplayName = "HttpResponseSeparate")]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(ThrowOther, DisplayName = "ThrowOther")]
		public void WhenInnerExceptionIsMissingDiagnosticIsTriggered(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.ThrowInnerException);
			VerifyDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(ThrowOther, "Dummy.Designer", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyDiagnostic(testCode, filePath);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new ThrowInnerExceptionAnalyzer();
		}
	}
}
