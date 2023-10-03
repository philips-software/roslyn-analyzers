// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="PreferReadOnlyParametersAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class PreferReadOnlyParametersAnalyzerTest : DiagnosticVerifier
	{
		private const string Correct = @"
using System.Collection.Generic;
namespace PreferReadOnlyTests {
	public class Program {
		public static void Main(List<string> args) {
			args.Clear();
		}
	}
}";

		private const string CorrectNoParameters = @"
using System.Collection.Generic;
namespace PreferReadOnlyTests {
	public class Program {
		public static void Main() {
			// Does nothing
		}
	}
}";

		private const string CorrectIndexer = @"
using System.Collection.Generic;
namespace PreferReadOnlyTests {
	public class Program {
		public static void Main(List<string> args) {
			args[3] = ""42"";
		}
	}
}";

		private const string CorrectInvocation = @"
using System.Collection.Generic;
namespace PreferReadOnlyTests {
	public class Program {
		public static int Main(List<string> args) {
			return IndexOf42(args);
		}
        private static int IndexOf42(List<string> list) {
            list.Add(""23"");
            return list.IndexOf(""42"");
        }
	}
}";

		private const string CorrectIEnumerable = @"
using System.Collection.Generic;
using System.Linq;
namespace PreferReadOnlyTests {
	public class Program {
		public static int Main(IEnumerable<string> args) {
			return args.FirstOrDefault(item => item  == ""42"");
		}
	}
}";

		private const string Wrong = @"
using System.Collection.Generic;
namespace PreferReadOnlyTests {
	public class Program {
		public static int Main(List<string> args) {
			return args.Indexof(""42"");
		}
	}
}";

		private const string WrongInvocation = @"
using System.Collection.Generic;
namespace PreferReadOnlyTests {
	public class Program {
		public static int Main(List<string> args) {
			return IndexOf42(args);
		}
        private static int IndexOf42(IReadOnlyList<string> list) {
            return list.IndexOf(""42"");
        }
	}
}";

		private const string WrongLinq = @"
using System.Collection.Generic;
using System.Linq;
namespace PreferReadOnlyTests {
	public class Program {
		public static int Main(IList<string> args) {
			return args.FirstOrDefault(item => item  == ""42"");
		}
	}
}";

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Correct, DisplayName = nameof(Correct)),
		 DataRow(CorrectNoParameters, DisplayName = nameof(CorrectNoParameters)),
		 DataRow(CorrectIndexer, DisplayName = nameof(CorrectIndexer)),
		 DataRow(CorrectInvocation, DisplayName = nameof(CorrectInvocation)),
		 DataRow(CorrectIEnumerable, DisplayName = nameof(CorrectIEnumerable))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(Wrong, DisplayName = nameof(Wrong))]
		[DataRow(WrongInvocation, DisplayName = nameof(WrongInvocation))]
		[DataRow(WrongLinq, DisplayName = nameof(WrongLinq))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenExceptionIsNotLoggedDiagnosticIsTriggeredAsync(string testCode)
		{
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(Wrong, "Dummy.g", DisplayName = "OutOfScopeSourceFile")]
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
			return new PreferReadOnlyParametersAnalyzer();
		}
	}
}
