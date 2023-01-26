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
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidEmptyTypeInitializerCodeFixProvider)), Shared]
	public class AvoidEmptyTypeInitializerCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Don't have empty constructors";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AvoidEmptyTypeInitializer));

		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the method declaration identified by the diagnostic.
			ConstructorDeclarationSyntax token = (ConstructorDeclarationSyntax)root.FindNode(diagnosticSpan);

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: Title,
					createChangedDocument: c => RemoveConstructor(context.Document, token, c),
					equivalenceKey: Title),
				diagnostic);
		}

		private async Task<Document> RemoveConstructor(Document document, ConstructorDeclarationSyntax ctor, CancellationToken c)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

			var leadingTrivia = ctor.GetLeadingTrivia().Where(x => !(x.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || x.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))).ToArray();
			var newCtor = ctor.WithLeadingTrivia(leadingTrivia);

			rootNode = rootNode.ReplaceNode(ctor, newCtor);

			ClassDeclarationSyntax cls = rootNode.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().First(x => x.Identifier.Text == ctor.Identifier.Text);

			var newConstructor = (ConstructorDeclarationSyntax)cls.Members.First(x => x.IsKind(SyntaxKind.ConstructorDeclaration) && ((ConstructorDeclarationSyntax)x).Modifiers.Any(SyntaxKind.StaticKeyword));

			var newRoot = rootNode.RemoveNode(newConstructor, SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepExteriorTrivia);

			return document.WithSyntaxRoot(newRoot);
		}
	}
}
