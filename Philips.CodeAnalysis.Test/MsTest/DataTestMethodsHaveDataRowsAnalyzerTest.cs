// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class DataTestMethodsHaveDataRowsAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DataTestMethodsHaveDataRowsAnalyzer();
		}

		protected override (string name, string content)[] GetAdditionalSourceCode()
		{
			string code = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class DerivedDataSourceAttribute : Attribute, ITestDataSource
{
		public IEnumerable<object[]> GetData(MethodInfo methodInfo) => Array.Empty<object[]>();
		string GetDisplayName(MethodInfo methodInfo, object[] data) => string.Empty;
}
";
			return new[] { ("DerivedDataSourceAttribute.cs", code) };
		}

		#endregion

		#region Public Interface

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DataTestMethodsMustHaveDataRows()
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
	[DataTestMethod]
	public void Foo() { }
}";

			VerifyDiagnostic(code, new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.DataTestMethodsHaveDataRows),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, null) }
			});
		}

		[DataRow("[DerivedDataSource]", false)]
		[DataRow("[DataRow(\"arg\")]", false)]
		[DataRow("[DynamicData(\"test\")]", false)]
		[DataRow("[DynamicData(\"test\"), DynamicData(\"test2\")]", true)]
		[DataRow("[DataRow(\"arg\"), DynamicData(\"test\"), DynamicData(\"test2\")]", true)]
		[DataRow("", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DataTestMethodsMustHaveDataRows2(string arg, bool isError)
		{
			const string template = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	{0}
	[DataTestMethod]
	public void Foo() {{ }}
}}";
			string code = string.Format(template, arg);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.DataTestMethodsHaveDataRows);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[DataRow("[DerivedDataSource]", false)]
		[DataRow("[DataRow(\"arg\")]", true)]
		[DataRow("[DynamicData(\"test\")]", true)]
		[DataRow("", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void TestMethodsMustNotHaveDataRows(string arg, bool isError)
		{
			const string template = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	{0}
	[TestMethod]
	public void Foo() {{ }}
}}";
			string code = string.Format(template, arg);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.DataTestMethodsHaveDataRows);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		#endregion
	}
}
