// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class TestMethodsMustHaveTheCorrectNumberOfArgumentsAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TestMethodsMustHaveTheCorrectNumberOfArgumentsAnalyzer();
		}

		#endregion

		#region Public Interface

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
				VerifyCSharpDiagnostic(string.Format(code, testType, parameterListString));
			}
			else
			{
				VerifyCSharpDiagnostic(string.Format(code, testType, parameterListString), new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustHaveTheCorrectNumberOfArguments),
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, null) },
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				});
			}
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
				List<string> dataRowParametersStrings = new List<string>();
				for (int i = 0; i < dataRowParameters; i++)
				{
					dataRowParametersStrings.Add(i.ToString());
				}

				if (hasDisplayName)
				{
					dataRowParametersStrings.Add("DisplayName = \"blah\"");
				}

				string dataRowText = string.Format($"[DataRow({string.Join(',', dataRowParametersStrings)})]");

				dataRow = dataRowText;
			}

			string code = string.Format(template, testType, parameterListString, dataRow, isDynamicData ? "[DynamicData(nameof(GetVariants))]" : string.Empty);

			if (isCorrect)
			{
				VerifyCSharpDiagnostic(code);
			}
			else
			{
				VerifyCSharpDiagnostic(code, DiagnosticResultHelper.Create(DiagnosticIds.TestMethodsMustHaveTheCorrectNumberOfArguments));
			}
		}

		#endregion
	}
}
