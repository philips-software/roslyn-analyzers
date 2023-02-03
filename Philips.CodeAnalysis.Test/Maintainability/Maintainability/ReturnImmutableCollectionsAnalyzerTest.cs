// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="ReturnImmutableCollectionsAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class ReturnImmutableCollectionsAnalyzerTest : CodeFixVerifier
	{
		private const string CorrectReadOnlyList = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public IReadOnlyList<int> MethodA() {
            return null;
        }
    }
}";

		private const string CorrectReadOnlyCollection = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public IReadOnlyCollection<int> MethodA() {
            return null;
        }
    }
}";

		private const string CorrectReadOnlyDictionary = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public IReadOnlyDictionary<string, int> MethodA() {
            return null;
        }
    }
}";

		private const string CorrectEnumerable = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public IEnumerable<int> MethodA() {
            return null;
        }
    }
}";

		private const string CorrectImmutableArray = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public ImmutableArray<int> MethodA() {
            return ImmutableArray<int>.Empty;
        }
    }
}";

		private const string CorrectPrivate = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        private ImmutableArray<int> MethodA() {
            return ImmutableArray<int>.Empty;
        }
    }
}";

		private const string CorrectProperty = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        private ImmutableArray<int> PropertyA { get; }
    }
}";

		private const string WrongList = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public List<int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongIList = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public IList<int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongQueue = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public Queue<int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongSortedList = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public SortedList<string, int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongStack = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public Stack<int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongDictionary = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public Dictionary<string, int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongIDictionary = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public IDictionary<string, int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongArray = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public int[] MethodA() {
            return null;
        }
    }
}";

		private const string WrongProperty = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public int[] PropertyA { get; }
    }
}";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectReadOnlyList, DisplayName = nameof(CorrectReadOnlyList)),
		 DataRow(CorrectReadOnlyCollection, DisplayName = nameof(CorrectReadOnlyCollection)),
		 DataRow(CorrectReadOnlyDictionary, DisplayName = nameof(CorrectReadOnlyDictionary)),
		 DataRow(CorrectEnumerable, DisplayName = nameof(CorrectEnumerable)),
		 DataRow(CorrectImmutableArray, DisplayName = nameof(CorrectImmutableArray)),
		 DataRow(CorrectPrivate, DisplayName = nameof(CorrectPrivate)),
		 DataRow(CorrectProperty, DisplayName = nameof(CorrectProperty))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongList, CorrectReadOnlyList, DisplayName = nameof(WrongList)),
		 DataRow(WrongIList, CorrectReadOnlyList, DisplayName = nameof(WrongIList)),
		 DataRow(WrongQueue, CorrectReadOnlyCollection, DisplayName = nameof(WrongQueue)),
		 DataRow(WrongSortedList, CorrectReadOnlyDictionary, DisplayName = nameof(WrongSortedList)),
		 DataRow(WrongStack, CorrectReadOnlyCollection, DisplayName = nameof(WrongStack)),
		 DataRow(WrongDictionary, CorrectReadOnlyDictionary, DisplayName = nameof(WrongDictionary)),
		 DataRow(WrongIDictionary, CorrectReadOnlyDictionary, DisplayName = nameof(WrongIDictionary)),
		 DataRow(WrongArray, CorrectReadOnlyList, DisplayName = nameof(WrongArray)),
		 DataRow(WrongProperty, null, DisplayName = nameof(WrongProperty))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenMismatchOfPlusMinusDiagnosticIsRaised(string testCode, string fixedCode) {
			var expected = DiagnosticResultHelper.Create(DiagnosticId.ReturnImmutableCollections);
			VerifyDiagnostic(testCode, expected);
			if (!string.IsNullOrEmpty(fixedCode))
			{
				VerifyFix(testCode, fixedCode);
			}
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifySuccessfulCompilation(WrongList, filePath);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
			return new ReturnImmutableCollectionsAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new ReturnImmutableCollectionsCodeFixProvider();
		}
	}
}
