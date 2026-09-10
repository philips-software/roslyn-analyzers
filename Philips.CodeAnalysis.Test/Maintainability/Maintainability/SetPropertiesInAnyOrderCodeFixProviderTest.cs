// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class SetPropertiesInAnyOrderCodeFixProviderTest : CodeFixVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ConvertPropertyWithCustomSetterToAutoproperty()
		{
			var given = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; set; }
        public int Two {
            set {
                One = value - 1;
            }
        }
    }
}";

			var expected = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; set; }
        public int Two { get; set; }
    }
}";

			await VerifyFix(given, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ConvertPropertyWithCustomSetterAndGetterToAutoproperty()
		{
			var given = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; set; }
        public int Two {
            get { return _two; }
            set {
                One = value - 1;
                _two = value;
            }
        }
        private int _two;
    }
}";

			var expected = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; set; }
        public int Two { get; set; }
        private int _two;
    }
}";

			await VerifyFix(given, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ConvertPropertyWithOnlyCustomSetterToAutoproperty()
		{
			var given = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; set; }
        public int Two {
            set {
                One = value;
            }
        }
    }
}";

			var expected = @"
namespace PropertiesinOrderTests {
    public class Number {
        public int One { get; set; }
        public int Two { get; set; }
    }
}";

			await VerifyFix(given, expected).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new SetPropertiesInAnyOrderCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new SetPropertiesInAnyOrderAnalyzer();
		}
	}
}