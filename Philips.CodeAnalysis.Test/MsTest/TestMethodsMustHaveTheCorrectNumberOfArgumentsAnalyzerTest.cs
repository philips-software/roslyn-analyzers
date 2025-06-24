// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
	public class TestMethodsMustHaveTheCorrectNumberOfArgumentsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestMethodsMustHaveTheCorrectNumberOfArgumentsAnalyzer();
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalSourceCode()
		{
			var code = @"
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
			return base.GetAdditionalSourceCode().Add(("DerivedDataSourceAttribute.cs", code));
		}


		[DataRow("[STATestMethod]", 0, true)]
		[DataRow("[STATestMethod]", 1, false)]
		[DataRow("[TestMethod]", 0, true)]
		[DataRow("[TestMethod]", 1, false)]
		[DataRow("[DataTestMethod]", 0, true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodsMustBeInTestClassAsync(string testType, int parameters, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	{0}
	public void Foo({1}) {{ }}
}}";

			var parameterListString = string.Empty;
			for (var i = 0; i < parameters; i++)
			{
				parameterListString = string.Format("{0}, int p{1}", parameterListString, i);
			}

			if (isCorrect)
			{
				await VerifySuccessfulCompilation(string.Format(code, testType, parameterListString)).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(string.Format(code, testType, parameterListString), new DiagnosticResult()
				{
					Id = DiagnosticId.TestMethodsMustHaveTheCorrectNumberOfArguments.ToId(),
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, null) },
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				}).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ParamsAreExemptTest()
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[DataTestMethod]
	[DataRow(1)]
	[DataRow(1,2,3)]
	public void Foo(int x, params int[] y) {{ }}
}}";

			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}


		[DataRow(0)]
		[DataRow(1)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DerivedDataSourcesShouldBeIgnoredAsync(int parameters)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	[DerivedDataSource]
	[DataTestMethod]
	public void Foo({0}) {{ }}
}}";

			var parameterListString = string.Empty;
			for (var i = 0; i < parameters; i++)
			{
				parameterListString = string.Format("{0}, int p{1}", parameterListString, i);
			}


			await VerifySuccessfulCompilation(string.Format(code, parameterListString)).ConfigureAwait(false);
		}

		private static IEnumerable<object[]> DataRowVariants()
		{
			const string DataTestMethod = "[DataTestMethod]";

			foreach (var hasDisplayName in new[] { true, false })
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestMethodsMustBeInTestClass2Async(string testType, int parameters, int dataRowParameters, bool isDynamicData, bool hasDisplayName, bool isCorrect)
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

			var parameterListStrings = new string[parameters];
			for (var i = 0; i < parameters; i++)
			{
				parameterListStrings[i] = $"int p{i}";
			}

			var parameterListString = string.Join(',', parameterListStrings);

			var dataRow = string.Empty;

			if (dataRowParameters >= 0)
			{
				List<string> dataRowParametersStrings = [];
				for (var i = 0; i < dataRowParameters; i++)
				{
					dataRowParametersStrings.Add(i.ToString());
				}

				if (hasDisplayName)
				{
					dataRowParametersStrings.Add("DisplayName = \"blah\"");
				}

				var dataRowText = $"[DataRow({string.Join(',', dataRowParametersStrings)})]";

				dataRow = dataRowText;
			}

			var code = string.Format(template, testType, parameterListString, dataRow, isDynamicData ? "[DynamicData(nameof(GetVariants))]" : string.Empty);

			if (isCorrect)
			{
				await VerifySuccessfulCompilation(code).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(code, DiagnosticId.TestMethodsMustHaveTheCorrectNumberOfArguments).ConfigureAwait(false);
			}
		}
	}
}
