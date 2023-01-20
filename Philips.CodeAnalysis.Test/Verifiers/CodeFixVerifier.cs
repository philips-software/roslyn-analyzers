// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Philips.CodeAnalysis.Test
{
	/// <summary>
	/// Superclass of all Unit tests made for diagnostics with codefixes.
	/// Contains methods used to verify correctness of codefixes
	/// </summary>
	public abstract partial class CodeFixVerifier : DiagnosticVerifier
	{
		/// <summary>
		/// Returns the codefix being tested (C#) - to be implemented in non-abstract class
		/// </summary>
		/// <returns>The CodeFixProvider to be used for CSharp code</returns>
		protected abstract CodeFixProvider GetCSharpCodeFixProvider();

		/// <summary>
		/// Returns the codefix being tested (VB) - to be implemented in non-abstract class
		/// </summary>
		/// <returns>The CodeFixProvider to be used for VisualBasic code</returns>
		protected virtual CodeFixProvider GetBasicCodeFixProvider()
		{
			return null;
		}

		/// <summary>
		/// Called to test a C# codefix when applied on the inputted string as a source
		/// </summary>
		/// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
		/// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
		/// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
		/// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
		protected void VerifyCSharpFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
		{
			var analyzer = GetCSharpDiagnosticAnalyzer();
			var codeFixProvider = GetCSharpCodeFixProvider();
			VerifyFix(LanguageNames.CSharp, analyzer, codeFixProvider, oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics);
		}

		/// <summary>
		/// Called to test a VB codefix when applied on the inputted string as a source
		/// </summary>
		/// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
		/// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
		/// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
		/// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
		protected void VerifyBasicFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
		{
			var analyzer = GetBasicDiagnosticAnalyzer();
			var codeFixProvider = GetBasicCodeFixProvider();
			VerifyFix(LanguageNames.VisualBasic, analyzer, codeFixProvider, oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics);
		}

		/// <summary>
		/// General verifier for codefixes.
		/// Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
		/// Then gets the string after the codefix is applied and compares it with the expected result.
		/// Note: If any codefix causes new diagnostics to show up, the test fails unless allowNewCompilerDiagnostics is set to true.
		/// </summary>
		/// <param name="language">The language the source code is in</param>
		/// <param name="analyzer">The analyzer to be applied to the source code</param>
		/// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
		/// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
		/// <param name="expectedSource">A class in the form of a string after the CodeFix was applied to it</param>
		/// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
		/// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
		private void VerifyFix(string language, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string oldSource, string expectedSource, int? codeFixIndex, bool allowNewCompilerDiagnostics)
		{
			var document = CreateDocument(oldSource, language);
			var analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });
			var compilerDiagnostics = GetCompilerDiagnostics(document);

			// Check if the found analyzer diagnostics are to be fixed by the given CodeFixProvider.
			if (analyzerDiagnostics.Any())
			{
				var analyzerDiagnosticIds = analyzerDiagnostics.Select(d => d.Id);
				var notFixableDiagnostics = codeFixProvider.FixableDiagnosticIds.Intersect(analyzerDiagnosticIds);
				Assert.IsTrue(notFixableDiagnostics.Any(),
					$"CodeFixProvider {codeFixProvider.GetType().Name} is not registered to fix the reported diagnostics: {string.Join(',', analyzerDiagnosticIds)}.");
			}
			
			var attempts = analyzerDiagnostics.Length;

			for (int i = 0; i < attempts; ++i)
			{
				var actions = new List<CodeAction>();
				var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, d) => actions.Add(a), CancellationToken.None);
				codeFixProvider.RegisterCodeFixesAsync(context).Wait();

				if (!actions.Any())
				{
					break;
				}

				if (codeFixIndex != null)
				{
					var codeAction1 = actions.ElementAt((int)codeFixIndex);
					document = ApplyFix(document, codeAction1);
					break;
				}

				var codeAction2 = actions.ElementAt(0);
				document = ApplyFix(document, codeAction2);
				analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });

				var newDiagnostics = GetCompilerDiagnostics(document);
				var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, newDiagnostics);

				//check if applying the code fix introduced any new compiler diagnostics
				if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
				{
					// Format and get the compiler diagnostics again so that the locations make sense in the output
					document = document.WithSyntaxRoot(Formatter.Format(document.GetSyntaxRootAsync().Result, Formatter.Annotation, document.Project.Solution.Workspace));
					var newDiagnostics2 = GetCompilerDiagnostics(document);
					newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, newDiagnostics2);

					Assert.IsTrue(false,
						string.Format("Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
							string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString())),
							document.GetSyntaxRootAsync().Result.ToFullString()));
				}

				//check if there are analyzer diagnostics left after the code fix
				if (!analyzerDiagnostics.Any())
				{
					break;
				}
			}

			//after applying all of the code fixes, compare the resulting string to the inputted one
			string actualSource = GetStringFromDocument(document);
			string[] actualSourceLines = actualSource.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			string[] expectedSourceLines = expectedSource.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(expectedSourceLines.Length, actualSourceLines.Length, @"The result's line code differs from the expected result's line code.");
			for (int i = 0; i < actualSourceLines.Length; i++)
			{
				// Trimming the lines, to ignore indentation differences.
				Assert.AreEqual(expectedSourceLines[i].Trim(), actualSourceLines[i].Trim(), $"Source line {i}");
			}
		}
	}
}
