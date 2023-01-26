// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestMethodsMustHaveTheCorrectNumberOfArgumentsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestMethodsMustHaveTheCorrectNumberOfArgumentsAnalyzer();
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


		[DataRow("[TestMethod]", 0, true)]
		[DataRow("[TestMethod]", 1, false)]
		[DataRow("[DataTestMethod]", 0, true)]
		[DataTestMethod]
		public void TestMethodsMustBeInTestClass(string testType, int parameters, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	{0}
	public void Foo({1}) {{ }}
}}";

			string parameterListString = string.Empty;
			for (int i = 0; i < parameters; i++)
			{
				parameterListString = string.Format("{0}, int p{1}", parameterListString, i);
			}

			if (isCorrect)
			{
				VerifySuccessfulCompilation(string.Format(code, testType, parameterListString));
			}
			else
			{
				VerifyDiagnostic(string.Format(code, testType, parameterListString), new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustHaveTheCorrectNumberOfArguments),
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, null) },
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				});
			}
		}

		[DataRow(0)]
		[DataRow(1)]
		[DataTestMethod]
		public void DerivedDataSourcesShouldBeIgnored(int parameters)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[DerivedDataSource]
	[DataTestMethod]
	public void Foo({0}) {{ }}
}}";

			string parameterListString = string.Empty;
			for (int i = 0; i < parameters; i++)
			{
				parameterListString = string.Format("{0}, int p{1}", parameterListString, i);
			}


			VerifySuccessfulCompilation(string.Format(code, parameterListString));
		}

		private static IEnumerable<object[]> DataRowVariants()
		{
			const string DataTestMethod = "[DataTestMethod]";

			foreach (bool hasDisplayName in new[] { true, false })
			{
				yield return new object[] { DataTestMethod, 1, 1, false, hasDisplayName, true };
				yield return new object[] { DataTestMethod, 1, 2, false, hasDisplayName, false };
				yield return new object[] { DataTestMethod, 2, 1, false, hasDisplayName, false };
				yield return new object[] { DataTestMethod, 1, -1, true, hasDisplayName, true };
				yield return new object[] { DataTestMethod, 1, 0, true, hasDisplayName, false };
				yield return new object[] { DataTestMethod, 1, 2, true, hasDisplayName, false };
				yield return new object[] { DataTestMethod, 2, 1, true, hasDisplayName, false };
			}
		}

		[DynamicData(nameof(DataRowVariants), DynamicDataSourceType.Method)]
		[DataTestMethod]
		public void TestMethodsMustBeInTestClass2(string testType, int parameters, int dataRowParameters, bool isDynamicData, bool hasDisplayName, bool isCorrect)
		{
			const string template = @"using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class Tests
{{
	{3}
	{2}
	{0}
	public void Foo({1}) {{ }}

	private static IEnumerable<object[]> GetVariants()
	{{ return Array.Empty<object[]>(); }}
}}";

			string[] parameterListStrings = new string[parameters];
			for (int i = 0; i < parameters; i++)
			{
				parameterListStrings[i] = $"int p{i}";
			}

			string parameterListString = string.Join(',', parameterListStrings);

			string dataRow = string.Empty;

			if (dataRowParameters >= 0)
			{
				List<string> dataRowParametersStrings = new();
				for (int i = 0; i < dataRowParameters; i++)
				{
					dataRowParametersStrings.Add(i.ToString());
				}

				if (hasDisplayName)
				{
					dataRowParametersStrings.Add("DisplayName = \"blah\"");
				}

				string dataRowText = $"[DataRow({string.Join(',', dataRowParametersStrings)})]";

				dataRow = dataRowText;
			}

			string code = string.Format(template, testType, parameterListString, dataRow, isDynamicData ? "[DynamicData(nameof(GetVariants))]" : string.Empty);

			if (isCorrect)
			{
				VerifySuccessfulCompilation(code);
			}
			else
			{
				VerifyDiagnostic(code, DiagnosticResultHelper.Create(DiagnosticIds.TestMethodsMustHaveTheCorrectNumberOfArguments));
			}
		}
	}
}
