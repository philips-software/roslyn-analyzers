// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Verifiers
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

		protected void VerifyDiagnostic(string source)
		{
			var analyzer = GetDiagnosticAnalyzer() as SingleDiagnosticAnalyzer;
			Assert.IsNotNull(analyzer, @"This overload is only supported for Analyzers that support a single DiagnosticId");
			VerifyDiagnostic(source, analyzer.DiagnosticId);
		}

		protected void VerifyDiagnostic(string source, DiagnosticId id)
		{
			var diagnosticResult = new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(id),
				Location = new DiagnosticResultLocation(null),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
			};
			VerifyDiagnostic(source, diagnosticResult);
		}

		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
		/// Note: input a DiagnosticResult for each Diagnostic expected
		/// </summary>
		/// <param name="source">A class in the form of a string to run the analyzer on</param>
		/// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
		protected void VerifyDiagnostic(string source, DiagnosticResult expected)
		{
			VerifyDiagnostic(source, null, new[] { expected });
		}

		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
		/// Note: input a DiagnosticResult for each Diagnostic expected
		/// </summary>
		/// <param name="source">A class in the form of a string to run the analyzer on</param>
		/// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
		protected void VerifyDiagnostic(string source, DiagnosticResult[] expected)
		{
			VerifyDiagnostic(source, null, expected);
		}

		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
		/// </summary>
		/// <param name="source">A class in the form of a string to run the analyzer on</param>
		protected void VerifySuccessfulCompilation(string source)
		{
			VerifyDiagnostic(source, Array.Empty<DiagnosticResult>());
		}

		protected void VerifySuccessfulCompilation(string source, string fileNamePrefix)
		{
			VerifyDiagnostic(source, fileNamePrefix, Array.Empty<DiagnosticResult>());
		}

		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the inputted file as a source
		/// </summary>
		/// <param name="path">The file on disk to run the analyzer on</param>
		protected void VerifySuccessfulCompilationFromFile(string path)
		{
			var content = File.ReadAllText(path);
			var fileName = Path.GetFileNameWithoutExtension(path);
			VerifyDiagnostic(content, fileName, Array.Empty<DiagnosticResult>());
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

			Assert.AreEqual(expectedCount, actualCount, FormatWrongDiagnosticCount(actualResults, analyzer, expectedCount, actualCount));

			for (int i = 0; i < expectedResults.Length; i++)
			{
				var actual = actualResults.ElementAt(i);
				var expected = expectedResults[i];

				if (expected.Line == -1 && expected.Column == -1)
				{
					Assert.AreEqual(Location.None, actual.Location,
							string.Format("Expected:\nA project diagnostic with No location\nActual:\n{0}",
							FormatDiagnostics(analyzer, actual)));
				}
				else
				{
					var first = expected.Locations.First();
					VerifyDiagnosticLocation(analyzer, actual, actual.Location, first);
					var additionalLocations = actual.AdditionalLocations.ToArray();

					Assert.AreEqual(expected.Locations.Length - 1, additionalLocations.Length,
							string.Format("Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
								expected.Locations.Length - 1, additionalLocations.Length,
								FormatDiagnostics(analyzer, actual)));

					for (int j = 0; j < additionalLocations.Length; ++j)
					{
						VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
					}
				}

				CheckDiagnostic(analyzer, actual, expected);
			}
		}

		private static void CheckDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic actual, DiagnosticResult expected)
        {
			Assert.AreEqual(expected.Id, actual.Id,
                    string.Format("Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Id, actual.Id, FormatDiagnostics(analyzer, actual)));

			Assert.AreEqual(expected.Severity, actual.Severity,
                    string.Format("Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Severity, actual.Severity, FormatDiagnostics(analyzer, actual)));

            var input = actual.GetMessage();
            if (expected.Message != null)
            {
                Assert.IsTrue(expected.Message.IsMatch(input),
					string.Format("Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Message, actual.GetMessage(), FormatDiagnostics(analyzer, actual)));
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
            FileLinePositionSpan actualSpan = CheckPath(analyzer, diagnostic, actual, expected);

            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0 && expected.Line.HasValue)
            {
                Assert.AreEqual(expected.Line, actualLinePosition.Line + 1,
					string.Format("Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Line, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0 &&
                expected.Column.HasValue &&
                expected.Column != -1)
            {
                Assert.AreEqual(expected.Column, actualLinePosition.Character + 1,
					string.Format("Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Column, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
            }

            var actualEndLinePosition = actualSpan.EndLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualEndLinePosition.Line > 0 && expected.EndLine.HasValue)
            {
                Assert.AreEqual(expected.EndLine, actualEndLinePosition.Line + 1,
					string.Format("Expected diagnostic to end on line \"{0}\" but actually ended on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.EndLine, actualEndLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualEndLinePosition.Character > 0 &&
                expected.EndColumn.HasValue &&
                expected.Column != -1)
            {
                Assert.AreEqual(expected.EndColumn, actualEndLinePosition.Character + 1,
					string.Format("Expected diagnostic to end at column \"{0}\" but actually ended at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.EndColumn, actualEndLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
            }

        }

        private static FileLinePositionSpan CheckPath(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            var actualSpan = actual.GetLineSpan();

            if (expected.Path != null)
            {
                Assert.IsTrue(actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                    string.Format("Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Path, actualSpan.Path, FormatDiagnostics(analyzer, diagnostic)));
            }

            return actualSpan;
        }
		#endregion

		#region Formatting Diagnostics

		private static string FormatWrongDiagnosticCount(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, int expectedCount, int actualCount)
		{
			string diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults.ToArray()) : "    NONE.";
			return string.Format("Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n", expectedCount, actualCount, diagnosticsOutput);
		}

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
                FormatDiagnostic(analyzer, diagnostics[i], builder, i == diagnostics.Length - 1);
            }
            return builder.ToString();
        }

        private static void FormatDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, StringBuilder builder, bool isLast)
        {
            builder.AppendLine("// " + diagnostic.ToString());

            var analyzerType = analyzer.GetType();
            var rules = analyzer.SupportedDiagnostics;

            foreach (var rule in rules)
            {
                if (rule != null && rule.Id == diagnostic.Id)
                {
                    var location = diagnostic.Location;
                    if (location == Location.None)
                    {
                        builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                    }
                    else
                    {
                        Assert.IsTrue(location.IsInSource,
                            $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostic}\r\n");

                        string resultMethodName = "GetCSharpResultAt";
                        var linePosition = diagnostic.Location.GetLineSpan().StartLinePosition;

                        builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                            resultMethodName,
                            linePosition.Line + 1,
                            linePosition.Character + 1,
                            analyzerType.Name,
                            rule.Id);
                    }

                    if (!isLast)
                    {
                        builder.Append(',');
                    }

                    builder.AppendLine();
                    break;
                }
            }
        }
        #endregion
    }
}
