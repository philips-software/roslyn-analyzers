// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidStaticMethodAnalyzer)), Shared]
	public class AvoidStaticMethodCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Remove static modifier from method";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.AvoidStaticMethods)); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the method declaration identified by the diagnostic.
			if (root != null)
			{
				SyntaxNode syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
				if (syntaxNode != null)
				{
					MethodDeclarationSyntax methodDeclarationList
						= syntaxNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

					// Register a code action that will invoke the fix.
					context.RegisterCodeFix(
						CodeAction.Create(
							title: Title,
							createChangedDocument: c => RemoveModifier(context.Document, methodDeclarationList, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}
		private async Task<Document> RemoveModifier(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxToken modifierToRemove = method.Modifiers.First(m => m.ValueText == @"static");
			SyntaxTokenList newModifiers = method.Modifiers.Remove(modifierToRemove);

			MethodDeclarationSyntax newMethod = method.WithModifiers(newModifiers);
			SyntaxNode newRoot = rootNode.ReplaceNode(method, newMethod);
			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}
