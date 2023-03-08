// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidEmptyTypeInitializerCodeFixProvider)), Shared]
	public class AvoidEmptyTypeInitializerCodeFixProvider : SingleDiagnosticCodeFixProvider<ConstructorDeclarationSyntax>
	{
		protected override string Title => "Don't have empty constructors";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidEmptyTypeInitializer;

		protected override async Task<Document> ApplyFix(Document document, ConstructorDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			SyntaxTrivia[] leadingTrivia = node.GetLeadingTrivia().Where(x => !(x.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || x.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))).ToArray();
			ConstructorDeclarationSyntax newCtor = node.WithLeadingTrivia(leadingTrivia);

			rootNode = rootNode.ReplaceNode(node, newCtor);

			ClassDeclarationSyntax cls = rootNode.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().First(x => x.Identifier.Text == node.Identifier.Text);

			var newConstructor = (ConstructorDeclarationSyntax)cls.Members.First(x => x.IsKind(SyntaxKind.ConstructorDeclaration) && ((ConstructorDeclarationSyntax)x).Modifiers.Any(SyntaxKind.StaticKeyword));

			SyntaxNode newRoot = rootNode.RemoveNode(newConstructor, SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepExteriorTrivia);

			return document.WithSyntaxRoot(newRoot);
		}
	}
}
