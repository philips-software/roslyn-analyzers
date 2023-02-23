// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class PassByRefAnalyzerTest : DiagnosticVerifier
	{
		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParameterNotWrittenToAsync(bool isWrittenTo)
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
		public async Task ParameterNotWrittenToStructAsync(bool isWrittenTo)
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
		public async Task ParameterNotWrittenToButRequiredForInterfaceAsync(bool isExplicit)
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
		public async Task ParameterNotWrittenToButRequiredForBaseClassAsync()
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
		public async Task ParameterNotWrittenToExpressionMethodAsync(bool isWrittenTo)
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
		public async Task ParameterNotWrittenToNestedMethodAsync()
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
		public async Task EmptyStatementMethodBodyAsync(string baseClass, bool isError)
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
		public async Task SingleStatementMethodBodyAsync(string baseClass, string statement, bool isError)
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


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PassByRefAnalyzer();
		}
	}
}
