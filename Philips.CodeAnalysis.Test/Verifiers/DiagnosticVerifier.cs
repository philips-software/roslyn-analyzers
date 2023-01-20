﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Philips.CodeAnalysis.Test
{
	/// <summary>
	/// Superclass of all Unit Tests for DiagnosticAnalyzers
	/// </summary>
	public abstract partial class DiagnosticVerifier
	{
		#region To be implemented by Test classes
		/// <summary>
		/// Get the Analyzer being tested - to be implemented in non-abstract class
		/// </summary>
		protected abstract DiagnosticAnalyzer GetDiagnosticAnalyzer();

		#endregion

		#region Verifier wrappers

		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
		/// Note: input a DiagnosticResult for each Diagnostic expected
		/// </summary>
		/// <param name="source">A class in the form of a string to run the analyzer on</param>
		/// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
		protected void VerifyDiagnostic(string source, string filenamePrefix, params DiagnosticResult[] expected)
		{
			var analyzer = GetDiagnosticAnalyzer();
			VerifyDiagnosticsInternal(new[] { source }, filenamePrefix, analyzer, expected);
		}

		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
		/// Note: input a DiagnosticResult for each Diagnostic expected
		/// </summary>
		/// <param name="source">A class in the form of a string to run the analyzer on</param>
		/// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
		protected void VerifyDiagnostic(string source, params DiagnosticResult[] expected)
		{
			VerifyDiagnostic(source, null, expected);
		}

        /// <summary>
        /// General method that gets a collection of actual diagnostics found in the source after the analyzer is run, 
        /// then verifies each of them.
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="filenamePrefix">The name of the source file, without the extension</param>
		/// <param name="analyzer">The analyzer to be run on the source code</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        private void VerifyDiagnosticsInternal(string[] sources, string filenamePrefix, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expected)
		{
			var diagnostics = GetSortedDiagnostics(sources, filenamePrefix, analyzer);
			VerifyDiagnosticResults(diagnostics, analyzer, expected);
		}

		#endregion

		#region Actual comparisons and verifications
		/// <summary>
		/// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
		/// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
		/// </summary>
		/// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
		/// <param name="analyzer">The analyzer that was being run on the sources</param>
		/// <param name="expectedResults">Diagnostic Results that should have appeared in the code</param>
		private static void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
		{
			int expectedCount = expectedResults.Length;
			int actualCount = actualResults.Count();

			if (expectedCount != actualCount)
			{
				var diagnostics = actualResults.ToArray();
				string diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, diagnostics) : "    NONE.";

				Assert.IsTrue(false,
					string.Format("Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n", expectedCount, actualCount, diagnosticsOutput));
			}

			for (int i = 0; i < expectedResults.Length; i++)
			{
				var actual = actualResults.ElementAt(i);
				var expected = expectedResults[i];

				if (expected.Line == -1 && expected.Column == -1)
				{
					if (actual.Location != Location.None)
					{
						Assert.IsTrue(false,
							string.Format("Expected:\nA project diagnostic with No location\nActual:\n{0}",
							FormatDiagnostics(analyzer, actual)));
					}
				}
				else
				{
					var first = expected.Locations.First();
					VerifyDiagnosticLocation(analyzer, actual, actual.Location, first);
					var additionalLocations = actual.AdditionalLocations.ToArray();

					if (additionalLocations.Length != expected.Locations.Length - 1)
					{
						Assert.IsTrue(false,
							string.Format("Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
								expected.Locations.Length - 1, additionalLocations.Length,
								FormatDiagnostics(analyzer, actual)));
					}

					for (int j = 0; j < additionalLocations.Length; ++j)
					{
						VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
					}
				}

				if (actual.Id != expected.Id)
				{
					Assert.IsTrue(false,
						string.Format("Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
							expected.Id, actual.Id, FormatDiagnostics(analyzer, actual)));
				}

				if (actual.Severity != expected.Severity)
				{
					Assert.IsTrue(false,
						string.Format("Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
							expected.Severity, actual.Severity, FormatDiagnostics(analyzer, actual)));
				}

				var input = actual.GetMessage();
				if (expected.Message != null && !expected.Message.IsMatch(input))
				{
					Assert.IsTrue(false,
						string.Format("Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
							expected.Message, actual.GetMessage(), FormatDiagnostics(analyzer, actual)));
				}
			}
		}

		/// <summary>
		/// Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
		/// </summary>
		/// <param name="analyzer">The analyzer that was being run on the sources</param>
		/// <param name="diagnostic">The diagnostic that was found in the code</param>
		/// <param name="actual">The Location of the Diagnostic found in the code</param>
		/// <param name="expected">The DiagnosticResultLocation that should have been found</param>
		private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
		{
			var actualSpan = actual.GetLineSpan();

			if (expected.Path != null)
			{
				Assert.IsTrue(actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
					string.Format("Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
						expected.Path, actualSpan.Path, FormatDiagnostics(analyzer, diagnostic)));
			}

			var actualLinePosition = actualSpan.StartLinePosition;

			// Only check line position if there is an actual line in the real diagnostic
			if (actualLinePosition.Line > 0 && expected.Line.HasValue)
			{
				if (actualLinePosition.Line + 1 != expected.Line)
				{
					Assert.IsTrue(false,
						string.Format("Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
							expected.Line, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
				}
			}

			// Only check column position if there is an actual column position in the real diagnostic
			if (actualLinePosition.Character > 0 && expected.Column.HasValue)
			{
				if (expected.Column != -1 && actualLinePosition.Character + 1 != expected.Column)
				{
					Assert.IsTrue(false,
						string.Format("Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
							expected.Column, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
				}
			}
		}
		#endregion

		#region Formatting Diagnostics
		/// <summary>
		/// Helper method to format a Diagnostic into an easily readable string
		/// </summary>
		/// <param name="analyzer">The analyzer that this verifier tests</param>
		/// <param name="diagnostics">The Diagnostics to be formatted</param>
		/// <returns>The Diagnostics formatted as a string</returns>
		private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
		{
			var builder = new StringBuilder();
			for (int i = 0; i < diagnostics.Length; ++i)
			{
				builder.AppendLine("// " + diagnostics[i].ToString());

				var analyzerType = analyzer.GetType();
				var rules = analyzer.SupportedDiagnostics;

				foreach (var rule in rules)
				{
					if (rule != null && rule.Id == diagnostics[i].Id)
					{
						var location = diagnostics[i].Location;
						if (location == Location.None)
						{
							builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
						}
						else
						{
							Assert.IsTrue(location.IsInSource,
								$"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

							string resultMethodName = "GetCSharpResultAt";
							var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

							builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
								resultMethodName,
								linePosition.Line + 1,
								linePosition.Character + 1,
								analyzerType.Name,
								rule.Id);
						}

						if (i != diagnostics.Length - 1)
						{
							builder.Append(',');
						}

						builder.AppendLine();
						break;
					}
				}
			}
			return builder.ToString();
		}
		#endregion
	}
}
