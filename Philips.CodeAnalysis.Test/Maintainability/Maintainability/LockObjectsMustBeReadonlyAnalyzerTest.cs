// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class LockObjectsMustBeReadonlyAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LockObjectsMustBeReadonlyAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new LockObjectsMustBeReadonlyCodeFixProvider();
		}

		[DataRow("static object _foo", true)]
		[DataRow("object _foo", true)]
		[DataRow("static readonly object _foo", false)]
		[DataRow("readonly object _foo", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyAsync(string field, bool isError)
		{
			const string template = @"using System;
class Foo
{{
	{0};

	public void Test()
	{{
		lock(_foo) {{ }}
	}}
}}
";
			if (isError)
			{
				await VerifyDiagnostic(string.Format(template, field)).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(template).ConfigureAwait(false);
			}
		}

		[DataRow("object foo", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyLocalVariablesAsync(string field, bool isError)
		{
			const string template = @"using System;
class Foo
{{
	public void Test()
	{{
		{0};
		lock(foo) {{ }}
	}}
}}
";
			if (isError)
			{
				await VerifyDiagnostic(string.Format(template, field)).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(template).ConfigureAwait(false);
			}
		}

		[DataRow("object foo", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyFunctionReturnAsync(string field, bool isError)
		{
			const string template = @"using System;
class Foo
{{
	public object GetFoo()
	{{
		return null;
	}}

	public void Test()
	{{
		{0} = GetFoo();
		lock(foo) {{ }}
	}}
}}
";
			if (isError)
			{
				await VerifyDiagnostic(string.Format(template, field)).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(template).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyPartialStatementAsync()
		{
			const string template = @"using System;
class Foo
{{
	private readonly object _foo;

	public void Test()
	{{
		lock(_foo)
	}}
}}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyPartialStatement2Async()
		{
			const string template = @"using System;
class Foo
{{
	private readonly object _foo;

	public void Test()
	{{
		lock
	}}
}}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyVerifyErrorMessageAsync()
		{
			const string template = @"using System;
class Foo
{{
	private object _foo;

	public void Test()
	{{
		lock(_foo) {{ }}
	}}
}}
";
			await VerifyDiagnostic(template, DiagnosticId.LocksShouldBeReadonly, regex: "'_foo'").ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyCodeFixPrivateFieldAsync()
		{
			const string original = @"using System;
class Foo
{
	private object _foo;

	public void Test()
	{
		lock(_foo) { }
	}
}
";
			const string expected = @"using System;
class Foo
{
	private readonly object _foo;

	public void Test()
	{
		lock(_foo) { }
	}
}
";
			await VerifyFix(original, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyCodeFixStaticFieldAsync()
		{
			const string original = @"using System;
class Foo
{
	static object _foo;

	public void Test()
	{
		lock(_foo) { }
	}
}
";
			const string expected = @"using System;
class Foo
{
	static readonly object _foo;

	public void Test()
	{
		lock(_foo) { }
	}
}
";
			await VerifyFix(original, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyCodeFixPrivateStaticFieldAsync()
		{
			const string original = @"using System;
class Foo
{
	private static object _foo;

	public void Test()
	{
		lock(_foo) { }
	}
}
";
			const string expected = @"using System;
class Foo
{
	private static readonly object _foo;

	public void Test()
	{
		lock(_foo) { }
	}
}
";
			await VerifyFix(original, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LockObjectsMustBeReadonlyCodeFixAlreadyReadonlyAsync()
		{
			const string code = @"using System;
class Foo
{
	private readonly object _foo;

	public void Test()
	{
		lock(_foo) { }
	}
}
";
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}
	}
}
