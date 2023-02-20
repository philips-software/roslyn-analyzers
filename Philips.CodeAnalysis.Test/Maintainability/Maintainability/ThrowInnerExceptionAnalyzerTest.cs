// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(ThrowOther, DisplayName = "ThrowOther")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenInnerExceptionIsMissingDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(ThrowOther, "Dummy.Designer", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggeredAsync(string testCode, string filePath)
		{
			await VerifySuccessfulCompilation(testCode, filePath).ConfigureAwait(false);
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
