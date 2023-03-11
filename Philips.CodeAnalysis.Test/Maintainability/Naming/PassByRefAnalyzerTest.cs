// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class PassByRefAnalyzerTest : DiagnosticVerifier
	{
		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParameterNotWrittenTo(bool isWrittenTo)
		{
			var content = $@"public class TestClass
{{
	static bool Foo(ref int i)
	{{
		{(isWrittenTo ? "i = 5;" : string.Empty)}
		return i == 1;
	}}

}}
";
			if (isWrittenTo)
			{
				await VerifySuccessfulCompilation(content).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(content).ConfigureAwait(false);
			}
		}

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParameterNotWrittenToStruct(bool isWrittenTo)
		{
			var content = $@"
public struct FooStruct
{{
public int i = 0;
}}

public class TestClass
{{
	static bool Foo(ref FooStruct obj)
	{{
		{(isWrittenTo ? "obj.i = 5;" : string.Empty)}
		return obj.i == 1;
	}}

}}
";

			if (isWrittenTo)
			{
				await VerifySuccessfulCompilation(content).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(content).ConfigureAwait(false);
			}
		}

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParameterNotWrittenToButRequiredForInterface(bool isExplicit)
		{
			var content = $@"
public interface IData
{{
	bool Foo(ref int i);
}}

public class TestClass : IData
{{
	{(isExplicit ? "bool IData." : "public bool")} Foo(ref int i)
	{{
		return i == 1;
	}}
}}
";

			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParameterNotWrittenToButRequiredForBaseClass()
		{
			var content = $@"
public abstract class Data
{{
	public abstract void Foo(ref int i);
}}

public class TestClass : Data
{{
	public override void Foo(ref int i)
	{{
		return i == 1;
	}}
}}
";

			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParameterNotWrittenToExpressionMethod(bool isWrittenTo)
		{
			var content = $@"public class TestClass
{{
	static bool Foo(ref int i) => {(isWrittenTo ? "i = 5" : "i == 5")};

}}
";

			if (isWrittenTo)
			{
				await VerifySuccessfulCompilation(content).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(content).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParameterNotWrittenToNestedMethod()
		{
			var content = $@"public class TestClass
{{
	static bool Foo(ref int i)
	{{
		i = 5;
		return i == 1;
	}}

	static void Bar(ref int i)
	{{
		Foo(ref i);
		return i == 1;
	}}

}}
";

			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}

		[DataRow(": Foo", false)]
		[DataRow("", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyStatementMethodBody(string baseClass, bool isError)
		{
			var content = $@"
public interface Foo {{ void Bar(ref int i); }}
public class TestClass {baseClass}
{{
	public void Bar(ref int i)
	{{
	}}
}}
";
			if (isError)
			{
				await VerifyDiagnostic(content).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(content).ConfigureAwait(false);
			}
		}

		[DataRow(": Foo", "i = 0", false)]
		[DataRow("", "i = 0", false)]
		[DataRow(": Foo", "_ = i.ToString()", false)]
		[DataRow("", "_ = i.ToString()", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SingleStatementMethodBody(string baseClass, string statement, bool isError)
		{
			var content = $@"
public interface Foo {{ void Bar(ref int i); }}
public class TestClass {baseClass}
{{
	public void Bar(ref int i)
	{{
		{statement};
	}}
}}
";
			if (isError)
			{
				await VerifyDiagnostic(content).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(content).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MethodWithoutParameters()
		{
			var content = @"
public class TestClass
{
	public void Bar()
	{
		int i = 0;
	}
}
";
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MethodWithoutRefParameters()
		{
			var content = @"
public class TestClass
{
	public void Bar(int i)
	{
		int j = i;
	}
}
";
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedCodeFilesShouldBeIgnored()
		{
			var givenText = $@"public class TestClass
{{
	static bool Foo(ref int i)
	{{
		return i == 1;
	}}

}}
";
			await VerifySuccessfulCompilation(givenText, "GlobalSuppressions").ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PassByRefAnalyzer();
		}
	}
}
