// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class LockObjectsMustBeReadonlyAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LockObjectsMustBeReadonlyAnalyzer();
		}

		[DataRow("static object _foo", true)]
		[DataRow("object _foo", true)]
		[DataRow("static readonly object _foo", false)]
		[DataRow("readonly object _foo", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LockObjectsMustBeReadonly(string field, bool isError)
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
				var result = DiagnosticResultHelper.Create(DiagnosticId.LocksShouldBeReadonly);
				VerifyDiagnostic(string.Format(template, field), result);
			}
			else
			{
				VerifySuccessfulCompilation(template);
			}
		}

		[DataRow("object foo", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LockObjectsMustBeReadonlyLocalVariables(string field, bool isError)
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
				var result = DiagnosticResultHelper.Create(DiagnosticId.LocksShouldBeReadonly);
				VerifyDiagnostic(string.Format(template, field), result);
			}
            else
            {
                VerifySuccessfulCompilation(template);
            }
		}

		[DataRow("object foo", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LockObjectsMustBeReadonlyFunctionReturn(string field, bool isError)
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
				var result = DiagnosticResultHelper.Create(DiagnosticId.LocksShouldBeReadonly);
				VerifyDiagnostic(string.Format(template, field), result);
			}
			else
			{
				VerifySuccessfulCompilation(template);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LockObjectsMustBeReadonlyPartialStatement()
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
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LockObjectsMustBeReadonlyPartialStatement2()
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
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LockObjectsMustBeReadonlyVerifyErrorMessage()
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
			var result = DiagnosticResultHelper.Create(DiagnosticId.LocksShouldBeReadonly);
			result.Message = new Regex("'_foo'");
			VerifyDiagnostic(template, result);
		}
	}
}
