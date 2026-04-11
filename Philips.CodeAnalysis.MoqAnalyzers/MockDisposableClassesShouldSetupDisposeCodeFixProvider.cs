// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MoqAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MockDisposableClassesShouldSetupDisposeCodeFixProvider)), Shared]
	public class MockDisposableClassesShouldSetupDisposeCodeFixProvider : SingleDiagnosticCodeFixProvider<ObjectCreationExpressionSyntax>
	{
		protected override string Title => "Use configured disposable mock type";

		protected override DiagnosticId DiagnosticId => DiagnosticId.MockDisposableObjectsShouldSetupDispose;

		protected override async Task<Document> ApplyFix(Document document, ObjectCreationExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			var configuredTypeName = GetPreferredDisposableMockType(properties);
			if (string.IsNullOrWhiteSpace(configuredTypeName))
			{
				return document;
			}

			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (rootNode == null)
			{
				return document;
			}

			if (node.Type is not GenericNameSyntax objectCreationGenericName)
			{
				return document;
			}

			TypeSyntax replacementTypeSyntax = CreateReplacementTypeSyntax(configuredTypeName, objectCreationGenericName.TypeArgumentList);

			TypeSyntax replacementType = replacementTypeSyntax.WithAdditionalAnnotations(Formatter.Annotation);

			SyntaxNode newRoot = rootNode;
			TypeSyntax declaredTypeToReplace = GetDeclaredTypeToReplace(node, objectCreationGenericName);

			if (declaredTypeToReplace != null)
			{
				newRoot = rootNode.ReplaceNodes(
					new SyntaxNode[] { node.Type, declaredTypeToReplace },
					(original, _) => replacementType.WithTriviaFrom(original));
			}
			else
			{
				newRoot = rootNode.ReplaceNode(
					node.Type,
					replacementType.WithTriviaFrom(node.Type));
			}

			return document.WithSyntaxRoot(newRoot);
		}

		private static TypeSyntax GetDeclaredTypeToReplace(ObjectCreationExpressionSyntax objectCreationSyntax, GenericNameSyntax originalObjectCreationType)
		{
			VariableDeclarationSyntax variableDeclarationSyntax = objectCreationSyntax.FirstAncestorOrSelf<VariableDeclarationSyntax>();
			if (variableDeclarationSyntax != null &&
				variableDeclarationSyntax.Type is GenericNameSyntax declaredGenericName &&
				declaredGenericName.Identifier.ValueText == originalObjectCreationType.Identifier.ValueText)
			{
				return variableDeclarationSyntax.Type;
			}

			PropertyDeclarationSyntax propertyDeclarationSyntax = objectCreationSyntax.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
			if (propertyDeclarationSyntax != null &&
				propertyDeclarationSyntax.Type is GenericNameSyntax propertyGenericName &&
				propertyGenericName.Identifier.ValueText == originalObjectCreationType.Identifier.ValueText)
			{
				return propertyDeclarationSyntax.Type;
			}

			FieldDeclarationSyntax fieldDeclarationSyntax = objectCreationSyntax.FirstAncestorOrSelf<FieldDeclarationSyntax>();
			if (fieldDeclarationSyntax != null &&
				fieldDeclarationSyntax.Declaration != null &&
				fieldDeclarationSyntax.Declaration.Type is GenericNameSyntax fieldGenericName &&
				fieldGenericName.Identifier.ValueText == originalObjectCreationType.Identifier.ValueText)
			{
				return fieldDeclarationSyntax.Declaration.Type;
			}

			return null;
		}

		private static string GetPreferredDisposableMockType(ImmutableDictionary<string, string> properties)
		{
			if (properties != null &&
				properties.TryGetValue(MockDisposableClassesShouldSetupDisposeAnalyzer.PreferredDisposableMockTypeProperty, out var configuredTypeName) &&
				!string.IsNullOrWhiteSpace(configuredTypeName))
			{
				return configuredTypeName.Trim();
			}

			return null;
		}

		private static TypeSyntax CreateReplacementTypeSyntax(string configuredTypeName, TypeArgumentListSyntax originalTypeArguments)
		{
			NameSyntax configuredNameSyntax = SyntaxFactory.ParseName(configuredTypeName);

			SimpleNameSyntax genericNameSyntax;
			if (configuredNameSyntax is QualifiedNameSyntax qualifiedNameSyntax)
			{
				genericNameSyntax = qualifiedNameSyntax.Right;
			}
			else if (configuredNameSyntax is AliasQualifiedNameSyntax aliasQualifiedNameSyntax)
			{
				genericNameSyntax = aliasQualifiedNameSyntax.Name;
			}
			else
			{
				genericNameSyntax = configuredNameSyntax as SimpleNameSyntax;
			}

			if (genericNameSyntax is GenericNameSyntax configuredGenericName)
			{
				// If user accidentally configured a generic form, overwrite its type args with the real ones.
				GenericNameSyntax rewrittenGenericName = configuredGenericName.WithTypeArgumentList(originalTypeArguments);

				if (configuredNameSyntax is QualifiedNameSyntax qualifiedName)
				{
					return qualifiedName.WithRight(rewrittenGenericName);
				}

				if (configuredNameSyntax is AliasQualifiedNameSyntax aliasQualifiedName)
				{
					return SyntaxFactory.AliasQualifiedName(aliasQualifiedName.Alias, rewrittenGenericName);
				}

				return rewrittenGenericName;
			}

			GenericNameSyntax appendedGenericName = SyntaxFactory.GenericName(genericNameSyntax.Identifier, originalTypeArguments);

			if (configuredNameSyntax is QualifiedNameSyntax qualifiedConfiguredName)
			{
				return qualifiedConfiguredName.WithRight(appendedGenericName);
			}

			if (configuredNameSyntax is AliasQualifiedNameSyntax aliasQualifiedConfiguredName)
			{
				return SyntaxFactory.AliasQualifiedName(aliasQualifiedConfiguredName.Alias, appendedGenericName);
			}

			return appendedGenericName;
		}
	}
}
