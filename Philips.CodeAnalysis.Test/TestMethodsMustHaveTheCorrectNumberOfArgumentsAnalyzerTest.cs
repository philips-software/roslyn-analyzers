// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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


		[DataRow("[DataTestMethod]", 1, 1, true)]
		[DataRow("[DataTestMethod]", 1, 2, false)]
		[DataRow("[DataTestMethod]", 2, 1, false)]
		[DataTestMethod]
		public void TestMethodsMustBeInTestClass2(string testType, int parameters, int dataRowParameters, bool isCorrect)
		{
			const string code = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{{
	{2}
	{0}
	public void Foo({1}) {{ }}
}}";

			string[] parameterListStrings = new string[parameters];
			for (int i = 0; i < parameters; i++)
			{
				parameterListStrings[i] = $"int p{i}";
			}

			string parameterListString = string.Join(',', parameterListStrings);

			string[] dataRowParametersStrings = new string[dataRowParameters];
			for (int i = 0; i < dataRowParameters; i++)
			{
				dataRowParametersStrings[i] = i.ToString();
			}

			string dataRow = string.Format($"[DataRow({string.Join(',', dataRowParametersStrings)})]");

			if (isCorrect)
			{
				VerifyCSharpDiagnostic(string.Format(code, testType, parameterListString, dataRow));
			}
			else
			{
				VerifyCSharpDiagnostic(string.Format(code, testType, parameterListString, dataRow), new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustHaveTheCorrectNumberOfArguments),
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, null) },
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				});
			}
		}

		#endregion
	}
}
