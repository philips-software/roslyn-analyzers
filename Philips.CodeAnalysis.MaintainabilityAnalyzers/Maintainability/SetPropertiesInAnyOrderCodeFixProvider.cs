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
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SetPropertiesInAnyOrderCodeFixProvider)), Shared]
	public class SetPropertiesInAnyOrderCodeFixProvider : SingleDiagnosticCodeFixProvider<AccessorDeclarationSyntax>
	{
		protected override string Title => "Convert to auto-property";

		protected override DiagnosticId DiagnosticId => DiagnosticId.SetPropertiesInAnyOrder;

		protected override async Task<Document> ApplyFix(Document document, AccessorDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			// Find the parent property declaration
			PropertyDeclarationSyntax propertyDeclaration = node.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
			if (propertyDeclaration == null)
			{
				return document;
			}

			// Create a simple auto-property with { get; set; }
			PropertyDeclarationSyntax newPropertyDeclaration = SyntaxFactory.PropertyDeclaration(
				attributeLists: propertyDeclaration.AttributeLists,
				modifiers: propertyDeclaration.Modifiers,
				type: propertyDeclaration.Type,
				explicitInterfaceSpecifier: propertyDeclaration.ExplicitInterfaceSpecifier,
				identifier: propertyDeclaration.Identifier,
				accessorList: SyntaxFactory.AccessorList(
					SyntaxFactory.List(new[]
					{
						SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
							.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
						SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
							.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
					}))
			).WithLeadingTrivia(propertyDeclaration.GetLeadingTrivia())
			.WithTrailingTrivia(propertyDeclaration.GetTrailingTrivia())
			.WithAdditionalAnnotations(Formatter.Annotation);

			// Replace the property in the syntax tree
			SyntaxNode newRoot = rootNode.ReplaceNode(propertyDeclaration, newPropertyDeclaration);

			return document.WithSyntaxRoot(newRoot);
		}
	}
}