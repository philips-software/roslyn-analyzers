// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	/// <summary>
	/// Test class for <see cref="EnforceAccessModifierAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class EnforceAccessModifierAnalyzerTest : DiagnosticVerifier
	{
		private const string Correct = @"
using System;

namespace AccessModifierUnitTests {
    public class Program {
        private int intField;
		protected string stringField;

		public static void Main(string[] args) {
        }

		protected void ProtectedMethod() {
		}

		internal void InternalMethod() {
		}

		protected internal void ProtectedInternalMethod() {
		}

		private void PrivateMethod() {
		}

		public string PublicReadOnlyProperty {
			get {
			}
		}

		private int PrivateProperty {
			get {
			}
			private set {
			}
		}
    }
}";


		private const string WrongClass = @"
using System;

namespace AccessModifierUnitTests {
    class Program {
    }
}";

		private const string WrongField = @"
using System;

namespace AccessModifierUnitTests {
    public class Program {
        int intField;
    }
}";
		
		private const string WrongMethod = @"
using System;

namespace AccessModifierUnitTests {
    public class Program {
		static void Main(string[] args) {
        }
    }
}";

		private const string WrongProperty = @"
using System;

namespace AccessModifierUnitTests {
    public class Program {
		int PrivateProperty {
			get {
			}
			set {
			}
		}
    }
}";

		/// <summary>
		/// No diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(Correct, DisplayName = nameof(Correct))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[TestMethod]
		[DataRow(WrongClass, DisplayName = nameof(WrongClass))]
		[DataRow(WrongField, DisplayName = nameof(WrongField))]
		[DataRow(WrongMethod, DisplayName = nameof(WrongMethod))]
		[DataRow(WrongProperty, DisplayName = nameof(WrongProperty))]
		public void WhenAccessModifierIsMissingDiagnosticIsTriggered(string testCode)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.EnforceAccessModifier);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow(WrongClass, "Dummy.Designer", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyCSharpDiagnostic(testCode, filePath);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new EnforceAccessModifierAnalyzer();
		}
	}
}
