// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
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
		public void ParameterNotWrittenTo(bool isWrittenTo)
		{
			string content = $@"public class TestClass
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
				VerifySuccessfulCompilation(content);
			}
			else
			{
				VerifyDiagnostic(content, DiagnosticResultHelper.Create(DiagnosticIds.AvoidPassByReference));
			}
		}

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ParameterNotWrittenToStruct(bool isWrittenTo)
		{
			string content = $@"
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
				VerifySuccessfulCompilation(content);
			}
			else
			{
				VerifyDiagnostic(content, DiagnosticResultHelper.Create(DiagnosticIds.AvoidPassByReference));
			}
		}

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ParameterNotWrittenToButRequiredForInterface(bool isExplicit)
		{
			string content = $@"
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

			VerifySuccessfulCompilation(content);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ParameterNotWrittenToButRequiredForBaseClass()
		{
			string content = $@"
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

			VerifySuccessfulCompilation(content);
		}

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ParameterNotWrittenToExpressionMethod(bool isWrittenTo)
		{
			string content = $@"public class TestClass
{{
	static bool Foo(ref int i) => {(isWrittenTo ? "i = 5" : "i == 5")};

}}
";

			if (isWrittenTo)
			{
				VerifySuccessfulCompilation(content);
			}
			else
			{
				VerifyDiagnostic(content, DiagnosticResultHelper.Create(DiagnosticIds.AvoidPassByReference));
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ParameterNotWrittenToNestedMethod()
		{
			string content = $@"public class TestClass
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

			VerifySuccessfulCompilation(content);
		}

		[DataRow(": Foo", false)]
		[DataRow("", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EmptyStatementMethodBody(string baseClass, bool isError)
		{
			string content = $@"
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
				VerifyDiagnostic(content, DiagnosticResultHelper.Create(DiagnosticIds.AvoidPassByReference));
			}
			else
			{
				VerifySuccessfulCompilation(content);
			}
		}

		[DataRow(": Foo", "i = 0", false)]
		[DataRow("", "i = 0", false)]
		[DataRow(": Foo", "_ = i.ToString()", false)]
		[DataRow("", "_ = i.ToString()", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void SingleStatementMethodBody(string baseClass, string statement, bool isError)
		{
			string content = $@"
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
				VerifyDiagnostic(content, DiagnosticResultHelper.Create(DiagnosticIds.AvoidPassByReference));
			}
			else
			{
				VerifySuccessfulCompilation(content);
			}
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PassByRefAnalyzer();
		}
	}
}
