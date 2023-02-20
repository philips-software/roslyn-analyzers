// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Contains unit tests for the AvoidTryParseWithoutCultureAnalyzer class.
	/// </summary>
	[TestClass]
	public class AvoidTryParseWithoutCultureAnalyzerTest : DiagnosticVerifier
	{
		private const string ClassString = @"
			using System;
			using System.Globalization;
			class Foo 
			{{
				public void Foo()
				{{
					{0}
				}}
			}}
			";

		private const string TestParserDefinition = @"
			class TestParser
			{{
				public static bool TryParse(string s, out TestParser tp)
				{{
					tp = new TestParser();
					return true;
				}}

				public static bool TryParse(string s, NumberStyles numberStyle, IFormatProvider format, out TestParser tp)
				{{
					tp = new TestParser();
					return true;
				}}

				public static bool TryParse(string s, CultureInfo culture, out TestParser tp)
				{{
					tp = new TestParser();
					return true;
				}}

				public static void TryParse() {{}}
			}}";


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidTryParseWithoutCultureAnalyzer();
		}


		[DataTestMethod]
		[DataRow("int.TryParse(\"3\", out int i);")]
		[DataRow("float.TryParse(\"3.00\", out float i);")]
		[DataRow("double.TryParse(\"3.00\", out double i);")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTryParseWithoutCultureForValueTypesAsync(string s)
		{
			string code = string.Format(ClassString, s);
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("int.TryParse(\"3\", NumberStyles.Any, CultureInfo.InvariantCulture, out int i);")]
		[DataRow("float.TryParse(\"3.00\", NumberStyles.Any, CultureInfo.InvariantCulture, out float i);")]
		[DataRow("double.TryParse(\"3.00\", NumberStyles.Any, CultureInfo.InvariantCulture, out double i);")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoNotFlagTryParseWithCultureForValueTypesAsync(string s)
		{
			string code = string.Format(ClassString, s);
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("TestParser.TryParse(\"3\", out TestParser tp);")]
		[DataRow("TestParser.TryParse();")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTryParseWithoutCultureForReferenceTypesAsync(string s)
		{
			string editorCode = string.Format(ClassString, s);
			string code = string.Concat(editorCode, TestParserDefinition);
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("TestParser.TryParse(\"3\", NumberStyles.Any, CultureInfo.InvariantCulture, out TestParser tp);")]
		[DataRow("TestParser.TryParse(\"3\", CultureInfo.InvariantCulture, out TestParser tp);")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoNotFlagTryParseWithCultureForReferenceTypesAsync(string s)
		{
			string editorCode = string.Format(ClassString, s);
			string code = string.Concat(editorCode, TestParserDefinition);
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}
	}
}
