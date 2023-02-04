// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestMethodNameCodeFixProvider)), Shared]
	public class TestMethodNameCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Remove invalid prefix";
		private const string TestLiteral = @"Test";
		private const string EnsureLiteral = @"Ensure";
		private const string VerifyLiteral = @"Verify";

		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.TestMethodName));

		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the method declaration identified by the diagnostic.
			if (root != null)
			{
				SyntaxNode syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
				if (syntaxNode != null)
				{
					var declaration = syntaxNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

					// Register a code action that will invoke the fix.
					context.RegisterCodeFix(
						CodeAction.Create(
							title: Title,
							createChangedSolution: c => DoNotBeginWithTest(context.Document, declaration, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		private async Task<Solution> DoNotBeginWithTest(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
		{
			// Compute new name.
			string name = methodDeclaration.Identifier.Text;

			while (name.Contains(TestLiteral))
			{
				name = name.Replace(TestLiteral, string.Empty);
			}
			while (name.Contains(EnsureLiteral))
			{
				name = name.Replace(EnsureLiteral, string.Empty);
			}
			while (name.Contains(VerifyLiteral))
			{
				name = name.Replace(VerifyLiteral, string.Empty);
			}

			name += TestLiteral;

			// Get the symbol representing the type to be renamed.
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			var typeSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);

			// Produce a new solution that has all references to that type renamed, including the declaration.
			var originalSolution = document.Project.Solution;
			var optionSet = originalSolution.Workspace.Options;
			var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, name, optionSet, cancellationToken).ConfigureAwait(false);

			// Return the new solution with the now-uppercase type name.
			return newSolution;
		}
	}
}