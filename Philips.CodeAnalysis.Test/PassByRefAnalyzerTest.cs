// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class PassByRefAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		#endregion

		#region Public Interface

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
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

			VerifyCSharpDiagnostic(content, isWrittenTo ? Array.Empty<DiagnosticResult>() : DiagnosticResultHelper.CreateArray(DiagnosticIds.AvoidPassByReference));
		}

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
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

			VerifyCSharpDiagnostic(content, isWrittenTo ? Array.Empty<DiagnosticResult>() : DiagnosticResultHelper.CreateArray(DiagnosticIds.AvoidPassByReference));
		}

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
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

			VerifyCSharpDiagnostic(content, Array.Empty<DiagnosticResult>());
		}

		[DataTestMethod]
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

			VerifyCSharpDiagnostic(content, Array.Empty<DiagnosticResult>());
		}

		[DataRow(true)]
		[DataRow(false)]
		[DataTestMethod]
		public void ParameterNotWrittenToExpressionMethod(bool isWrittenTo)
		{
			string content = $@"public class TestClass
{{
	static bool Foo(ref int i) => {(isWrittenTo ? "i = 5" : "i == 5")};

}}
";

			VerifyCSharpDiagnostic(content, isWrittenTo ? Array.Empty<DiagnosticResult>() : DiagnosticResultHelper.CreateArray(DiagnosticIds.AvoidPassByReference));
		}

		[TestMethod]
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

			VerifyCSharpDiagnostic(content, Array.Empty<DiagnosticResult>());
		}


		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new PassByRefAnalyzer();

		#endregion
	}
}
