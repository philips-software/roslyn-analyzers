// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
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
		private const string Start = @"start";
		private const string End = @"end";

		private static readonly Regex WildcardRegex =
			new(".*", RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1));
		#region To be implemented by Test classes
		/// <summary>
		/// Get the Analyzer being tested - to be implemented in non-abstract class
		/// </summary>
		protected abstract DiagnosticAnalyzer GetDiagnosticAnalyzer();

		#endregion

		#region Verifier wrappers

		protected async Task VerifyDiagnostic(string source, string filenamePrefix = null, string assemblyName = null, string regex = ".*", int? line = null)
		{
			var analyzer = GetDiagnosticAnalyzer() as SingleDiagnosticAnalyzer;
			Assert.IsNotNull(analyzer, @"This overload is only supported for Analyzers that support a single DiagnosticId");
			await VerifyDiagnostic(source, analyzer.DiagnosticId, filenamePrefix, assemblyName, regex, line).ConfigureAwait(false);
		}

		protected async Task VerifyDiagnostic(string source, DiagnosticId id, string filenamePrefix = null, string assemblyName = null, string regex = ".*", int? line = null)
		{
			var diagnosticResult = new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(id),
				Location = new DiagnosticResultLocation(line),
				Message = new Regex(regex, RegexOptions.Singleline, TimeSpan.FromSeconds(1)),
				Severity = DiagnosticSeverity.Error,
			};
			var analyzer = GetDiagnosticAnalyzer();
			await VerifyDiagnosticsInternal(new[] { source }, filenamePrefix, assemblyName, analyzer, new[] { diagnosticResult }).ConfigureAwait(false);
		}

		protected async Task VerifyDiagnostic(string source, int count)
		{
			Assert.IsTrue(count > 1, "Only use this overload when your test expects the same Diagnostic multiple times.");
			var analyzer = GetDiagnosticAnalyzer() as SingleDiagnosticAnalyzer;
			Assert.IsNotNull(analyzer, @"This overload is only for Analyzers that support a single DiagnosticId");

			DiagnosticResult[] diagnosticResults = new DiagnosticResult[count];
			for (int i = 0; i < count; i++)
			{
				var diagnosticResult = new DiagnosticResult()
				{
					Id = analyzer.Id,
					Location = new DiagnosticResultLocation(null),
					Message = WildcardRegex,
					Severity = DiagnosticSeverity.Error,
				};
				diagnosticResults[i] = diagnosticResult;
			}
			await VerifyDiagnosticsInternal(new[] { source }, null, null, analyzer, diagnosticResults).ConfigureAwait(false);
		}

		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
		/// Note: input a DiagnosticResult for each Diagnostic expected
		/// </summary>
		/// <param name="source">A class in the form of a string to run the analyzer on</param>
		/// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
		/// <param name="filenamePrefix">The name of the source file, without the extension</param>
		/// <param name="assemblyName">The name of the resulting assembly of the compilation, without the extension</param>
		protected async Task VerifyDiagnostic(string source, DiagnosticResult expected, string filenamePrefix = null, string assemblyName = null)
		{
			var analyzer = GetDiagnosticAnalyzer();
			await VerifyDiagnosticsInternal(new[] { source }, filenamePrefix, assemblyName, analyzer, new[] { expected }).ConfigureAwait(false);
		}

		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
		/// Note: input a DiagnosticResult for each Diagnostic expected
		/// </summary>
		/// <param name="source">A class in the form of a string to run the analyzer on</param>
		/// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
		/// <param name="filenamePrefix">The name of the source file, without the extension</param>
		/// <param name="assemblyName">The name of the resulting assembly of the compilation, without the extension</param>
		protected async Task VerifyDiagnostic(string source, DiagnosticResult[] expected, string filenamePrefix = null, string assemblyName = null)
		{
			Assert.IsTrue(expected.Length > 0, @"Specify a diagnostic. If you expect compilation to succeed, call VerifySuccessfulCompilation instead.");
			Assert.IsTrue(expected.Length > 1, @$"Use the overload that doesn't use an array of {nameof(DiagnosticResult)}s.");

			var analyzer = GetDiagnosticAnalyzer();
			await VerifyDiagnosticsInternal(new[] { source }, filenamePrefix, assemblyName, analyzer, expected).ConfigureAwait(false);
		}


		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
		/// </summary>
		/// <param name="source">A class in the form of a string to run the analyzer on</param>
		/// <param name="filenamePrefix">The name of the source file, without the extension</param>
		/// <param name="assemblyName">The name of the resulting assembly of the compilation, without the extension</param>
		protected async Task VerifySuccessfulCompilation(string source, string filenamePrefix = null, string assemblyName = null)
		{
			var analyzer = GetDiagnosticAnalyzer();
			await VerifyDiagnosticsInternal(new[] { source }, filenamePrefix, assemblyName, analyzer, Array.Empty<DiagnosticResult>()).ConfigureAwait(false);
		}

		/// <summary>
		/// Called to test a C# DiagnosticAnalyzer when applied on the inputted file as a source
		/// </summary>
		/// <param name="path">The file on disk to run the analyzer on</param>
		protected async Task VerifySuccessfulCompilationFromFile(string path)
		{
			var content = File.ReadAllText(path);
			var fileName = Path.GetFileNameWithoutExtension(path);
			await VerifySuccessfulCompilation(content, fileName).ConfigureAwait(false);
		}

		/// <summary>
		/// General method that gets a collection of actual diagnostics found in the source after the analyzer is run, 
		/// then verifies each of them.
		/// </summary>
		/// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
		/// <param name="filenamePrefix">The name of the source file, without the extension</param>
		/// <param name="assemblyName">The name of the resulting assembly of the compilation, without the extension</param>
		/// <param name="analyzer">The analyzer to be run on the source code</param>
		/// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
		private async Task VerifyDiagnosticsInternal(string[] sources, string filenamePrefix, string assemblyName, DiagnosticAnalyzer analyzer, DiagnosticResult[] expected)
		{
			var diagnostics = await GetSortedDiagnostics(sources, filenamePrefix, assemblyName, analyzer).ConfigureAwait(false);
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
		private void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, DiagnosticResult[] expectedResults)
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

					Assert.AreEqual(expected.Locations.Count - 1, additionalLocations.Length,
							string.Format("Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
								expected.Locations.Count - 1, additionalLocations.Length,
								FormatDiagnostics(analyzer, actual)));

					for (int j = 0; j < additionalLocations.Length; ++j)
					{
						var expectedLocation = expected.Locations.ElementAt(j + 1);
						VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expectedLocation);
					}
				}

				CheckDiagnostic(analyzer, actual, expected);
			}
		}

		private void CheckDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic actual, DiagnosticResult expected)
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

		private void CheckLine(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, int actualLinePosition, int? expectedLine, string startOrEnd)
		{
			// Only check line position if there is an actual line in the real diagnostic
			if (actualLinePosition > 0 && expectedLine.HasValue)
			{
				Assert.AreEqual(expectedLine, actualLinePosition + 1,
					$"Expected diagnostic to {startOrEnd} on line \"{expectedLine}\" was actually on line \"{actualLinePosition + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");
			}
		}

		private void CheckColumn(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, int actualCharacterPosition, int? expectedColumn, string startOrEnd)
		{
			// Only check column position if there is an actual column position in the real diagnostic
			if (actualCharacterPosition > 0 &&
				expectedColumn.HasValue &&
				expectedColumn != -1)
			{
				Assert.AreEqual(expectedColumn, actualCharacterPosition + 1,
					$"Expected diagnostic to {startOrEnd} at column \"{expectedColumn}\" was actually at column \"{actualCharacterPosition + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");
			}
		}


		/// <summary>
		/// Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
		/// </summary>
		/// <param name="analyzer">The analyzer that was being run on the sources</param>
		/// <param name="diagnostic">The diagnostic that was found in the code</param>
		/// <param name="actual">The Location of the Diagnostic found in the code</param>
		/// <param name="expected">The DiagnosticResultLocation that should have been found</param>
		private void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
		{
			FileLinePositionSpan actualSpan = CheckPath(analyzer, diagnostic, actual, expected);
			CheckLine(analyzer, diagnostic, actualSpan.StartLinePosition.Line, expected.Line, Start);
			CheckColumn(analyzer, diagnostic, actualSpan.StartLinePosition.Character, expected.Column, Start);
			CheckLine(analyzer, diagnostic, actualSpan.EndLinePosition.Line, expected.EndLine, End);
			CheckColumn(analyzer, diagnostic, actualSpan.EndLinePosition.Character, expected.EndColumn, End);
		}

		private FileLinePositionSpan CheckPath(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
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

		private string FormatWrongDiagnosticCount(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, int expectedCount, int actualCount)
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
		private string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
		{
			var builder = new StringBuilder();
			for (int i = 0; i < diagnostics.Length; ++i)
			{
				FormatDiagnostic(analyzer, diagnostics[i], builder, i == diagnostics.Length - 1);
			}
			return builder.ToString();
		}

		private void FormatDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, StringBuilder builder, bool isLast)
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
