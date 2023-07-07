// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Verifiers
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
		protected abstract CodeFixProvider GetCodeFixProvider();

		/// <summary>
		/// Checks if the specified <see cref="FixAllProvider"/> is as expected.
		/// </summary>
		/// <param name="fixAllProvider">The <see cref="FixAllProvider"/> instance returned by the code fixer.</param>
		protected virtual void AssertFixAllProvider(FixAllProvider fixAllProvider)
		{
			Assert.AreSame(WellKnownFixAllProviders.BatchFixer, fixAllProvider);
		}

		/// <summary>
		/// Called to test a C# codefix when applied on the inputted string as a source
		/// </summary>
		/// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
		/// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
		/// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
		/// <param name="shouldAllowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
		protected async Task VerifyFix(string oldSource, string newSource, int? codeFixIndex = null, bool shouldAllowNewCompilerDiagnostics = false)
		{
			DiagnosticAnalyzer analyzer = GetDiagnosticAnalyzer();
			CodeFixProvider codeFixProvider = GetCodeFixProvider();
			await VerifyFix(analyzer, codeFixProvider, oldSource, newSource, codeFixIndex, shouldAllowNewCompilerDiagnostics, FixAllScope.Custom).ConfigureAwait(false);
		}

		protected async Task VerifyFixAll(string oldSource, string newSource, int? codeFixIndex = null, bool shouldAllowNewCompilerDiagnostics = false)
		{
			DiagnosticAnalyzer analyzer = GetDiagnosticAnalyzer();
			CodeFixProvider codeFixProvider = GetCodeFixProvider();

			foreach (FixAllScope scope in new FixAllScope[] { FixAllScope.Solution, FixAllScope.Project, FixAllScope.Document })
			{
				await VerifyFix(analyzer, codeFixProvider, oldSource, newSource, codeFixIndex, shouldAllowNewCompilerDiagnostics, scope).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// General verifier for codefixes.
		/// Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
		/// Then gets the string after the codefix is applied and compares it with the expected result.
		/// Note: If any codefix causes new diagnostics to show up, the test fails unless allowNewCompilerDiagnostics is set to true.
		/// </summary>
		/// <param name="analyzer">The analyzer to be applied to the source code</param>
		/// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
		/// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
		/// <param name="expectedSource">A class in the form of a string after the CodeFix was applied to it</param>
		/// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
		/// <param name="shouldAllowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
		/// <param name="scope">Scope for the FixAllProvider. </param>
		private async Task VerifyFix(DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string oldSource, string expectedSource, int? codeFixIndex, bool shouldAllowNewCompilerDiagnostics, FixAllScope scope)
		{
			Microsoft.CodeAnalysis.Document document = CreateDocument(oldSource);
			IEnumerable<Microsoft.CodeAnalysis.Diagnostic> analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzer, new[] { document }).ConfigureAwait(false);
			IEnumerable<Microsoft.CodeAnalysis.Diagnostic> compilerDiagnostics = await GetCompilerDiagnostics(document).ConfigureAwait(false);

			// Check if the found analyzer diagnostics are to be fixed by the given CodeFixProvider.
			IEnumerable<string> analyzerDiagnosticIds = analyzerDiagnostics.Select(d => d.Id);
			if (analyzerDiagnostics.Any())
			{
				IEnumerable<string> notFixableDiagnostics = codeFixProvider.FixableDiagnosticIds.Intersect(analyzerDiagnosticIds);
				Assert.IsTrue(notFixableDiagnostics.Any(),
					$"CodeFixProvider {codeFixProvider.GetType().Name} is not registered to fix the reported diagnostics: {string.Join(',', analyzerDiagnosticIds)}.");
			}

			var attempts = analyzerDiagnostics.Count();
			for (var i = 0; i < attempts && analyzerDiagnostics.Any(); ++i)
			{
				var actions = new List<CodeAction>();
				Microsoft.CodeAnalysis.Diagnostic firstDiagnostic = analyzerDiagnostics.First();
				var context = new CodeFixContext(document, firstDiagnostic, (a, d) => actions.Add(a), CancellationToken.None);
				codeFixProvider.RegisterCodeFixesAsync(context).Wait();

				if (!actions.Any())
				{
					break;
				}


				if (scope == FixAllScope.Custom) // I.e., if not a FixAll
				{
					if (codeFixIndex != null)
					{
						CodeAction codeAction1 = actions.ElementAt((int)codeFixIndex);
						document = await ApplyFix(document, codeAction1).ConfigureAwait(false);
						break;
					}

					CodeAction codeAction2 = actions.ElementAt(0);
					document = await ApplyFix(document, codeAction2).ConfigureAwait(false);
				}
				else
				{
					FixAllContext.DiagnosticProvider diagnosticProvider = new TestDiagnosticProvider(analyzerDiagnostics, document);
					FixAllProvider fixAllProvider = codeFixProvider.GetFixAllProvider();
					FixAllContext fixAllContext = new(document, codeFixProvider, scope, actions[0].EquivalenceKey, analyzerDiagnosticIds, diagnosticProvider, CancellationToken.None);
					CodeAction fixAllAction = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
					document = await ApplyFix(document, fixAllAction).ConfigureAwait(false);
				}

				analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzer, new[] { document }).ConfigureAwait(false);

				IEnumerable<Microsoft.CodeAnalysis.Diagnostic> newDiagnostics = await GetCompilerDiagnostics(document).ConfigureAwait(false);
				IEnumerable<Microsoft.CodeAnalysis.Diagnostic> newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, newDiagnostics);

				//check if applying the code fix introduced any new compiler diagnostics
				if (!shouldAllowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
				{
					Microsoft.CodeAnalysis.SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(false);
					// Format and get the compiler diagnostics again so that the locations make sense in the output
					document = document.WithSyntaxRoot(Formatter.Format(syntaxRoot, Formatter.Annotation, document.Project.Solution.Workspace));
					IEnumerable<Microsoft.CodeAnalysis.Diagnostic> newDiagnostics2 = await GetCompilerDiagnostics(document).ConfigureAwait(false);
					newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, newDiagnostics2);

					IEnumerable<string> newDiagnosticsString = newCompilerDiagnostics.Select(d => d.ToString());
					var rootString = syntaxRoot?.ToFullString();
					Assert.Fail(
						$"Fix introduced new compiler diagnostics:\r\n{string.Join("\r\n", newDiagnosticsString)}\r\n\r\nNew document:\r\n{rootString}\r\n");
				}
			}

			// After applying all of the code fixes, there shouldn't be any problems remaining
			var numberOfDiagnostics = analyzerDiagnostics.Count();
			Assert.IsTrue(shouldAllowNewCompilerDiagnostics || !analyzerDiagnostics.Any(), $@"After applying the fix, there still exists {numberOfDiagnostics} diagnostic(s): {Helper.ToPrettyList(analyzerDiagnostics)}");

			// After applying all of the code fixes, compare the resulting string to the inputted one
			var actualSource = await GetStringFromDocument(document).ConfigureAwait(false);
			var actualSourceLines = actualSource.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			var expectedSourceLines = expectedSource.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(expectedSourceLines.Length, actualSourceLines.Length, @"The result's line code differs from the expected result's line code.");
			for (var i = 0; i < actualSourceLines.Length; i++)
			{
				// Trimming the lines, to ignore indentation differences.
				Assert.AreEqual(expectedSourceLines[i].Trim(), actualSourceLines[i].Trim(), $"Source line {i}");
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CheckFixAllProvider()
		{
			// Arrange
			FixAllProvider fixAllProvider = GetCodeFixProvider().GetFixAllProvider();
			// Assert
			AssertFixAllProvider(fixAllProvider);
		}
	}
}
