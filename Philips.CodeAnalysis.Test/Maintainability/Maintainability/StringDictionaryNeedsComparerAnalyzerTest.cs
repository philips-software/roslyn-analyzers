// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class StringDictionaryNeedsComparerAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new StringDictionaryNeedsComparerAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DictionaryWithStringKeyWithoutComparerTriggersAsync()
		{
			const string template = @"
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var dict = new Dictionary<string, int>();
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.StringDictionaryNeedsComparer).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DictionaryWithStringKeyWithComparerDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var dict = new Dictionary<string, int>(StringComparer.Ordinal);
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task HashSetWithStringKeyWithoutComparerTriggersAsync()
		{
			const string template = @"
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var set = new HashSet<string>();
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.StringDictionaryNeedsComparer).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task HashSetWithStringKeyWithComparerDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ConcurrentDictionaryWithStringKeyWithoutComparerTriggersAsync()
		{
			const string template = @"
using System.Collections.Concurrent;
class Foo
{
	public void Test()
	{
		var dict = new ConcurrentDictionary<string, int>();
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.StringDictionaryNeedsComparer).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ConcurrentDictionaryWithStringKeyWithComparerDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using System.Collections.Concurrent;
class Foo
{
	public void Test()
	{
		var dict = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SortedDictionaryWithStringKeyWithoutComparerTriggersAsync()
		{
			const string template = @"
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var dict = new SortedDictionary<string, int>();
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.StringDictionaryNeedsComparer).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SortedDictionaryWithStringKeyWithComparerDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var dict = new SortedDictionary<string, int>(StringComparer.Ordinal);
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SortedSetWithStringKeyWithoutComparerTriggersAsync()
		{
			const string template = @"
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var set = new SortedSet<string>();
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.StringDictionaryNeedsComparer).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SortedSetWithStringKeyWithComparerDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ImmutableDictionaryCreateWithStringKeyWithoutComparerTriggersAsync()
		{
			const string template = @"
using System.Collections.Immutable;
class Foo
{
	public void Test()
	{
		var dict = ImmutableDictionary.Create<string, int>();
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.StringDictionaryNeedsComparer).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ImmutableDictionaryCreateWithStringKeyWithComparerDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using System.Collections.Immutable;
class Foo
{
	public void Test()
	{
		var dict = ImmutableDictionary.Create<string, int>(StringComparer.Ordinal);
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DictionaryWithNonStringKeyDoesNotTriggerAsync()
		{
			const string template = @"
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var dict = new Dictionary<int, string>();
		var dict2 = new Dictionary<object, int>();
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task HashSetWithNonStringKeyDoesNotTriggerAsync()
		{
			const string template = @"
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var set = new HashSet<int>();
		var set2 = new HashSet<object>();
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DictionaryWithCapacityParameterWithoutComparerTriggersAsync()
		{
			const string template = @"
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var dict = new Dictionary<string, int>(10);
	}
}
";
			await VerifyDiagnostic(template, DiagnosticId.StringDictionaryNeedsComparer).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DictionaryWithCapacityAndComparerDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using System.Collections.Generic;
class Foo
{
	public void Test()
	{
		var dict = new Dictionary<string, int>(10, StringComparer.Ordinal);
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}
	}
}