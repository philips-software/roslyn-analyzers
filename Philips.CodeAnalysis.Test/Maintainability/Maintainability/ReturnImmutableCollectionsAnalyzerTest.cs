// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="ReturnImmutableCollectionsAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class ReturnImmutableCollectionsAnalyzerTest : DiagnosticVerifier
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
        public IReadOnlyDictionary<string,int> MethodA() {
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

		private const string WrongCollection = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public Collection<int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongICollection = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public ICollection<int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongDictionary = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public Dictionary<string,int> MethodA() {
            return null;
        }
    }
}";

		private const string WrongIDictionary = @"
using System.Collections.Generic;
namespace ReturnImmutableTests {
    public class Number {
        public IDictionary<string,int> MethodA() {
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
		[TestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(CorrectReadOnlyList, DisplayName = nameof(CorrectReadOnlyList)),
		 DataRow(CorrectReadOnlyCollection, DisplayName = nameof(CorrectReadOnlyCollection)),
		 DataRow(CorrectReadOnlyDictionary, DisplayName = nameof(CorrectReadOnlyDictionary)),
		 DataRow(CorrectEnumerable, DisplayName = nameof(CorrectEnumerable)),
		 DataRow(CorrectImmutableArray, DisplayName = nameof(CorrectImmutableArray)),
		 DataRow(CorrectPrivate, DisplayName = nameof(CorrectPrivate)),
		 DataRow(CorrectProperty, DisplayName = nameof(CorrectProperty))]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[TestMethod]
		[DataRow(WrongList, DisplayName = nameof(WrongList)),
		 DataRow(WrongIList, DisplayName = nameof(WrongIList)),
		 DataRow(WrongCollection, DisplayName = nameof(WrongCollection)),
		 DataRow(WrongICollection, DisplayName = nameof(WrongICollection)),
		 DataRow(WrongDictionary, DisplayName = nameof(WrongDictionary)),
		 DataRow(WrongIDictionary, DisplayName = nameof(WrongIDictionary)),
		 DataRow(WrongArray, DisplayName = nameof(WrongArray)),
		 DataRow(WrongProperty, DisplayName = nameof(WrongProperty))]
		public void WhenMismatchOfPlusMinusDiagnosticIsRaised(string testCode) {
			var expected = DiagnosticResultHelper.Create(DiagnosticIds.ReturnImmutableCollections);
			VerifyDiagnostic(testCode, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[TestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifyDiagnostic(WrongList, filePath);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
			return new ReturnImmutableCollectionsAnalyzer();
		}
	}
}
