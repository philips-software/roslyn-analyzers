using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Contains unit tests for the AvoidTryParseWithoutCultureAnalyzer class.
	/// </summary>
	[TestClass]
	public class AvoidTryParseWithoutCultureAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

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

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidTryParseWithoutCultureAnalyzer();
		}

		private DiagnosticResultLocation GetBaseDiagnosticLocation(int rowOffset = 0, int columnOffset = 0)
		{
			return new DiagnosticResultLocation("Test.cs", 8 + rowOffset, 6 + columnOffset);
		}

		#endregion

		#region Public Interface

		#endregion

		#region Test Methods

		[DataTestMethod]
		[DataRow("int.TryParse(\"3\", out int i);")]
		[DataRow("float.TryParse(\"3.00\", out float i);")]
		[DataRow("double.TryParse(\"3.00\", out double i);")]
		public void AvoidTryParseWithoutCultureForValueTypes(string s)
		{
			string code = string.Format(ClassString, s);
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidTryParseWithoutCulture),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation()
				}
			};

			VerifyDiagnostic(code, expected);
		}

		[DataTestMethod]
		[DataRow("int.TryParse(\"3\", NumberStyles.Any, CultureInfo.InvariantCulture, out int i);")]
		[DataRow("float.TryParse(\"3.00\", NumberStyles.Any, CultureInfo.InvariantCulture, out float i);")]
		[DataRow("double.TryParse(\"3.00\", NumberStyles.Any, CultureInfo.InvariantCulture, out double i);")]
		public void DoNotFlagTryParseWithCultureForValueTypes(string s)
		{
			string code = string.Format(ClassString, s);
			VerifySuccessfulCompilation(code);
		}

		[DataTestMethod]
		[DataRow("TestParser.TryParse(\"3\", out TestParser tp);")]
		[DataRow("TestParser.TryParse();")]
		public void AvoidTryParseWithoutCultureForReferenceTypes(string s)
		{
			string editorCode = string.Format(ClassString, s);
			string code = string.Concat(editorCode, TestParserDefinition);
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidTryParseWithoutCulture),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation()
				}
			};

			VerifyDiagnostic(code, expected);
		}

		[DataTestMethod]
		[DataRow("TestParser.TryParse(\"3\", NumberStyles.Any, CultureInfo.InvariantCulture, out TestParser tp);")]
		[DataRow("TestParser.TryParse(\"3\", CultureInfo.InvariantCulture, out TestParser tp);")]

		public void DoNotFlagTryParseWithCultureForReferenceTypes(string s)
		{
			string editorCode = string.Format(ClassString, s);
			string code = string.Concat(editorCode, TestParserDefinition);
			VerifySuccessfulCompilation(code);
		}

		#endregion
	}
}
