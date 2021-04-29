// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.
 
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	/// <summary>
	/// Test class for <see cref="AvoidThreadStartAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidThreadStartAnalyzerTest : DiagnosticVerifier
	{

		private const string Correct = @"
    using System.Threading;

    namespace ThreadStartUnitTests {
        public class Program {
            private void DoWork() {
            }

            public Thread Main() {
                ThreadStart work = this.DoWork;
                var worker = ThreadPool.QueueUserWorkItem(work);
                return worker;
            }
        }
    }";

		private const string CorrectWithParameter = @"
    using System.Threading;

    namespace ThreadStartUnitTests {
        public class Program {
            private void DoWork(object data) {
            }

            public Thread Main() {
                var worker = ThreadPool.QueueUserWorkItem((ParameterizedThreadStart)this.DoWork);
                return worker;
            }
        }
    }";

		private const string Instance = @"
    using System.Threading;

    namespace ThreadStartUnitTests {
        public class Program {
            private void DoWork() {
            }

            public Thread Main() {
                ThreadStart work = this.DoWork;
                var worker = new Thread(work);
                return worker;
            }
        }
    }";

		private const string Static = @"
    using System.Threading;

    namespace ThreadStartUnitTests {
        public class Program {
            private static void DoWork() {
            }

            public Thread Main() {
                ThreadStart work = Program.DoWork;
                var worker = new Thread(work);
                return worker;
            }
        }
    }";

		private const string InstanceWithParameter = @"
    using System.Threading;

    namespace ThreadStartUnitTests {
        public class Program {
            private void DoWork(object data) {
            }

            public Thread Main() {
                ParameterizedThreadStart work = this.DoWork;
                var worker = new Thread(work);
                return worker;
            }
        }
    }";

		private const string StaticWithParameter = @"
    using System.Threading;

    namespace ThreadStartUnitTests {
        public class Program {
            private static void DoWork(object data) {
            }

            public Thread Main() {
                ParameterizedThreadStart work = Program.DoWork;                
                var worker = new Thread(work);
                return worker;
            }
        }
    }";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(Correct, DisplayName = "Correct"),
		 DataRow(CorrectWithParameter, DisplayName = "CorrectWithParameter")]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(Instance, DisplayName = "Instance"),
		 DataRow(InstanceWithParameter, DisplayName = "InstanceWithParameter"),
		 DataRow(Static, DisplayName = "Static"),
		 DataRow(StaticWithParameter, DisplayName = "StaticWithParameter")]
		public void WhenThreadIsCreatedDirectlyDiagnosticIsRaised(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.AvoidThreadStart);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow(Instance, "Test.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyCSharpDiagnostic(testCode, filePath);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidThreadStartAnalyzer();
		}
	}
}
