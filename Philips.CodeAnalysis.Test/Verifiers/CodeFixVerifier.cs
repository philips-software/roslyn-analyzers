// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
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
		protected void VerifyFix(string oldSource, string newSource, int? codeFixIndex = null, bool shouldAllowNewCompilerDiagnostics = false)
		{
			var analyzer = GetDiagnosticAnalyzer();
			var codeFixProvider = GetCodeFixProvider();
			VerifyFix(analyzer, codeFixProvider, oldSource, newSource, codeFixIndex, shouldAllowNewCompilerDiagnostics, FixAllScope.Custom);
		}

		protected void VerifyFixAll(string oldSource, string newSource, int? codeFixIndex = null, bool shouldAllowNewCompilerDiagnostics = false)
		{
			var analyzer = GetDiagnosticAnalyzer();
			var codeFixProvider = GetCodeFixProvider();

			foreach (FixAllScope scope in new FixAllScope[] { FixAllScope.Solution, FixAllScope.Project, FixAllScope.Document})
			{
				VerifyFix(analyzer, codeFixProvider, oldSource, newSource, codeFixIndex, shouldAllowNewCompilerDiagnostics, scope);
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
        private void VerifyFix(DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string oldSource, string expectedSource, int? codeFixIndex, bool shouldAllowNewCompilerDiagnostics, FixAllScope scope)
        {
            var document = CreateDocument(oldSource);
            var analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });
            var compilerDiagnostics = GetCompilerDiagnostics(document);

            // Check if the found analyzer diagnostics are to be fixed by the given CodeFixProvider.
            var analyzerDiagnosticIds = analyzerDiagnostics.Select(d => d.Id);
            if (analyzerDiagnostics.Any())
            {
                var notFixableDiagnostics = codeFixProvider.FixableDiagnosticIds.Intersect(analyzerDiagnosticIds);
                Assert.IsTrue(notFixableDiagnostics.Any(),
                    $"CodeFixProvider {codeFixProvider.GetType().Name} is not registered to fix the reported diagnostics: {string.Join(',', analyzerDiagnosticIds)}.");
            }

            var attempts = analyzerDiagnostics.Count();
            for (int i = 0; i < attempts && analyzerDiagnostics.Any(); ++i)
            {
                var actions = new List<CodeAction>();
                var firstDiagnostic = analyzerDiagnostics.First();
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
                        var codeAction1 = actions.ElementAt((int)codeFixIndex);
                        document = ApplyFix(document, codeAction1);
                        break;
                    }

                    var codeAction2 = actions.ElementAt(0);
                    document = ApplyFix(document, codeAction2);
                }
                else
                {
                    FixAllContext.DiagnosticProvider diagnosticProvider = new TestDiagnosticProvider(analyzerDiagnostics, document);
                    FixAllProvider fixAllProvider = codeFixProvider.GetFixAllProvider();
                    FixAllContext fixAllContext = new(document, codeFixProvider, scope, actions[0].EquivalenceKey, analyzerDiagnosticIds, diagnosticProvider, CancellationToken.None);
                    CodeAction fixAllAction = fixAllProvider.GetFixAsync(fixAllContext).Result;
                    document = ApplyFix(document, fixAllAction);
                }

                analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });

                var newDiagnostics = GetCompilerDiagnostics(document);
                var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, newDiagnostics);

                //check if applying the code fix introduced any new compiler diagnostics
                if (!shouldAllowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(document.GetSyntaxRootAsync().Result, Formatter.Annotation, document.Project.Solution.Workspace));
                    var newDiagnostics2 = GetCompilerDiagnostics(document);
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, newDiagnostics2);

                    Assert.Fail(
                        string.Format("Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
                            string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString())),
                            document.GetSyntaxRootAsync().Result.ToFullString()));
                }
            }

            //after applying all of the code fixes, there shouldn't be any problems remaining
            Helper helper = new();
            var numberOfDiagnostics = analyzerDiagnostics.Count();
            Assert.IsTrue(shouldAllowNewCompilerDiagnostics || !analyzerDiagnostics.Any(), $@"After applying the fix, there still exists {numberOfDiagnostics} diagnostic(s): {helper.ToPrettyList(analyzerDiagnostics)}");

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

        [TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CheckFixAllProvider()
		{
			// Arrange
			var fixAllProvider = GetCodeFixProvider().GetFixAllProvider();
			// Assert
			AssertFixAllProvider(fixAllProvider);
		}
	}
}
