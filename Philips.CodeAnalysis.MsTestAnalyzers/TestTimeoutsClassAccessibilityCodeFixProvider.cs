// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

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

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestTimeoutsClassAccessibilityCodeFixProvider)), Shared]
	public class TestTimeoutsClassAccessibilityCodeFixProvider : SingleDiagnosticCodeFixProvider<ClassDeclarationSyntax>
	{
		protected override string Title => "Make TestTimeouts class internal";

		protected override DiagnosticId DiagnosticId => DiagnosticId.TestTimeoutsClassShouldBeInternal;

		protected override async Task<Document> ApplyFix(Document document, ClassDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			// Remove problematic modifiers and ensure internal is present
			SyntaxTokenList modifiers = node.Modifiers;

			// Remove public, sealed (when public), and static (when internal) modifiers
			var newModifiers = modifiers
				.Where(m => m.Kind() is not SyntaxKind.PublicKeyword and not SyntaxKind.StaticKeyword)
				.ToList();

			// Check if we need to add internal
			var hasInternal = newModifiers.Any(m => m.Kind() == SyntaxKind.InternalKeyword);

			if (!hasInternal)
			{
				// Add internal modifier at the beginning
				newModifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.InternalKeyword));
			}

			// Create new class declaration with updated modifiers
			ClassDeclarationSyntax newClass = node.WithModifiers(SyntaxFactory.TokenList(newModifiers));

			// Replace the node in the root
			SyntaxNode root = rootNode.ReplaceNode(node, newClass);
			Document newDocument = document.WithSyntaxRoot(root);

			return newDocument;
		}
	}
}
