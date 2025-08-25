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

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LockObjectsMustBeReadonlyCodeFixProvider)), Shared]
	public class LockObjectsMustBeReadonlyCodeFixProvider : SingleDiagnosticCodeFixProvider<IdentifierNameSyntax>
	{
		protected override string Title => "Add readonly modifier to field";

		protected override DiagnosticId DiagnosticId => DiagnosticId.LocksShouldBeReadonly;

		protected override async Task<Document> ApplyFix(Document document, IdentifierNameSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

			// Get the symbol referenced by the identifier
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
			if (symbolInfo.Symbol is not IFieldSymbol fieldSymbol)
			{
				return document;
			}

			// Use the field symbol's declaration reference to find the actual syntax node
			SyntaxReference declaringSyntaxReference = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
			if (declaringSyntaxReference == null)
			{
				return document;
			}

			SyntaxNode declaringSyntax = await declaringSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
			FieldDeclarationSyntax fieldDeclaration = declaringSyntax.FirstAncestorOrSelf<FieldDeclarationSyntax>();

			if (fieldDeclaration == null)
			{
				return document;
			}

			// Add readonly modifier if not already present
			SyntaxTokenList modifiers = fieldDeclaration.Modifiers;
			if (!modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)))
			{
				SyntaxToken readonlyModifier = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword).WithTrailingTrivia(SyntaxFactory.Space);

				// Insert readonly after access modifiers but before static
				SyntaxTokenList newModifiers = modifiers;
				var insertIndex = 0;

				// Find appropriate position for readonly modifier
				for (var i = 0; i < modifiers.Count; i++)
				{
					if (modifiers[i].IsKind(SyntaxKind.PublicKeyword) ||
						modifiers[i].IsKind(SyntaxKind.PrivateKeyword) ||
						modifiers[i].IsKind(SyntaxKind.ProtectedKeyword) ||
						modifiers[i].IsKind(SyntaxKind.InternalKeyword))
					{
						insertIndex = i + 1;
					}
					else if (modifiers[i].IsKind(SyntaxKind.StaticKeyword))
					{
						insertIndex = i + 1;
						break;
					}
				}

				newModifiers = newModifiers.Insert(insertIndex, readonlyModifier);
				FieldDeclarationSyntax newFieldDeclaration = fieldDeclaration.WithModifiers(newModifiers);

				SyntaxNode newRoot = rootNode.ReplaceNode(fieldDeclaration, newFieldDeclaration);
				return document.WithSyntaxRoot(newRoot);
			}

			return document;
		}
	}
}