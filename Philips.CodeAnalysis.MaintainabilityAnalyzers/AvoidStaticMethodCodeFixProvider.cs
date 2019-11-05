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

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidStaticMethodAnalyzer)), Shared]
	public class AvoidStaticMethodCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Remove static modifier from method";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AvoidStaticMethods)); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the method declaration identified by the diagnostic.
			MethodDeclarationSyntax methodDeclarationList
					= root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: Title,
					createChangedDocument: c => RemoveModifier(context.Document, methodDeclarationList, c),
					equivalenceKey: Title),
				diagnostic);
		}
		private async Task<Document> RemoveModifier(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			var modifierToRemove = method.Modifiers.Where(m => m.ValueText == @"static").First();
			var newModifiers = ((SyntaxTokenList)method.Modifiers).Remove(modifierToRemove);

			MethodDeclarationSyntax newMethod = method.WithModifiers(newModifiers);
			SyntaxNode newRoot = rootNode.ReplaceNode(method, newMethod);
			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}
