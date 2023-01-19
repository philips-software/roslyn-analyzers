// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="AvoidMagicNumbersAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AvoidMagicNumbersAnalyzerTest : DiagnosticVerifier
	{
		private const string CorrectZero = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private int NoMagic = 0;
    }
}";

		private const string CorrectOne = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private int NoMagic = 1;
    }
}";

		private const string CorrectUnsigned = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private uint NoMagic = 0;
    }
}";

		private const string CorrectLong = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private long NoMagic = 1;
    }
}";

		private const string CorrectZeroFloat = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private double NoMagic = 0d;
    }
}";

		private const string CorrectOneFloat = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private float NoMagic = 1.0f;
    }
}";

		private const string CorrectField = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private static int Magic = 5;
    }
}";

		private const string CorrectConst = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private const int Magic = 5;
    }
}";

		private const string CorrectPowerOf10 = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private int Magic = 100;
    }
}";

		private const string CorrectPowerOf2 = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private int Magic = 16;
    }
}";

		private const string CorrectAngle = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private int Magic = 90;
    }
}";

		private const string CorrectInEnum = @"
namespace DontUseMagicNumbersTests {
    public enum NumberEnmeration {
        None = 0,
        Magic = 3
    }
}";

        private const string WrongInstanceField = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        private int Magic = 5;
    }
}";

		private const string WrongConstLocal = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        public void Main() {
            const int Magic = 5;
        }
    }
}";

		private const string WrongLocal = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        public void Main() {
            int Magic = 5;
        }
    }
}";

		private const string WrongPropertyInitializer = @"
namespace DontUseMagicNumbersTests {
    public class Number {
        public int Magic { get; } = 5;
    }
}";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectZero, DisplayName = nameof(CorrectZero)),
		 DataRow(CorrectOne, DisplayName = nameof(CorrectOne)),
		 DataRow(CorrectUnsigned, DisplayName = nameof(CorrectUnsigned)),
		 DataRow(CorrectLong, DisplayName = nameof(CorrectLong)),
		 DataRow(CorrectZeroFloat, DisplayName = nameof(CorrectZeroFloat)),
		 DataRow(CorrectOneFloat, DisplayName = nameof(CorrectOneFloat)),
		 DataRow(CorrectField, DisplayName = nameof(CorrectField)),
		 DataRow(CorrectConst, DisplayName = nameof(CorrectConst)),
		 DataRow(CorrectPowerOf10, DisplayName = nameof(CorrectPowerOf10)),
		 DataRow(CorrectPowerOf2, DisplayName = nameof(CorrectPowerOf2)),
		 DataRow(CorrectAngle, DisplayName = nameof(CorrectAngle)),
		 DataRow(CorrectInEnum, DisplayName = nameof(CorrectInEnum))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(WrongInstanceField, DisplayName = nameof(WrongInstanceField)),
		 DataRow(WrongConstLocal, DisplayName = nameof(WrongConstLocal)),
		 DataRow(WrongLocal, DisplayName = nameof(WrongLocal)),
		 DataRow(WrongPropertyInitializer, DisplayName = nameof(WrongPropertyInitializer))]
		public void WhenMismatchOfPlusMinusDiagnosticIsRaised(string testCode) {
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.AvoidMagicNumbers);
			VerifyCSharpDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifyCSharpDiagnostic(WrongLocal, filePath);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
			return new AvoidMagicNumbersAnalyzer();
		}
	}
}
