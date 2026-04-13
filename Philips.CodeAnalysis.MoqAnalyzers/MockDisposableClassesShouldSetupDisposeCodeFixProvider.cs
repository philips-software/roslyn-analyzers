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
	public class MockDisposableClassesShouldSetupDisposeCodeFixProvider : SingleDiagnosticCodeFixProvider<ExpressionSyntax>
	{
		protected override string Title => "Use configured disposable mock type";

		protected override DiagnosticId DiagnosticId => DiagnosticId.MockDisposableObjectsShouldSetupDispose;

		protected override async Task<Document> ApplyFix(Document document, ExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
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

			GenericNameSyntax objectCreationGenericName = null;

			if (node is ObjectCreationExpressionSyntax explicitCreation &&
				explicitCreation.Type is GenericNameSyntax explicitGeneric)
			{
				objectCreationGenericName = explicitGeneric;
			}
			else if (node is ImplicitObjectCreationExpressionSyntax)
			{
				TypeSyntax declaredType = GetDeclaredTypeFromContext(node);
				objectCreationGenericName = declaredType as GenericNameSyntax;
			}

			if (objectCreationGenericName == null)
			{
				return document;
			}

			TypeSyntax replacementTypeSyntax = CreateReplacementTypeSyntax(configuredTypeName, objectCreationGenericName.TypeArgumentList);
			TypeSyntax replacementType = replacementTypeSyntax.WithAdditionalAnnotations(Formatter.Annotation);

			SyntaxNode newRoot;
			TypeSyntax declaredTypeToReplace = GetDeclaredTypeToReplace(node, objectCreationGenericName);

			if (node is ImplicitObjectCreationExpressionSyntax)
			{
				if (declaredTypeToReplace == null)
				{
					return document;
				}

				newRoot = rootNode.ReplaceNode(
					declaredTypeToReplace,
					replacementType.WithTriviaFrom(declaredTypeToReplace));
			}
			else if (node is ObjectCreationExpressionSyntax explicitObjectCreation)
			{
				if (declaredTypeToReplace != null)
				{
					newRoot = rootNode.ReplaceNodes(
						new SyntaxNode[] { explicitObjectCreation.Type, declaredTypeToReplace },
						(original, _) => replacementType.WithTriviaFrom(original));
				}
				else
				{
					newRoot = rootNode.ReplaceNode(
						explicitObjectCreation.Type,
						replacementType.WithTriviaFrom(explicitObjectCreation.Type));
				}
			}
			else
			{
				return document;
			}

			return document.WithSyntaxRoot(newRoot);
		}

		private static TypeSyntax GetDeclaredTypeFromContext(SyntaxNode node)
		{
			VariableDeclarationSyntax variableDeclarationSyntax = node.FirstAncestorOrSelf<VariableDeclarationSyntax>();
			if (variableDeclarationSyntax != null)
			{
				return variableDeclarationSyntax.Type;
			}

			PropertyDeclarationSyntax propertyDeclarationSyntax = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
			if (propertyDeclarationSyntax != null)
			{
				return propertyDeclarationSyntax.Type;
			}

			return null;
		}

		private static TypeSyntax GetDeclaredTypeToReplace(SyntaxNode node, GenericNameSyntax originalObjectCreationType)
		{
			VariableDeclarationSyntax variableDeclarationSyntax = node.FirstAncestorOrSelf<VariableDeclarationSyntax>();
			if (variableDeclarationSyntax != null &&
				variableDeclarationSyntax.Type is GenericNameSyntax declaredGenericName &&
				declaredGenericName.Identifier.ValueText == originalObjectCreationType.Identifier.ValueText)
			{
				return variableDeclarationSyntax.Type;
			}

			PropertyDeclarationSyntax propertyDeclarationSyntax = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
			if (propertyDeclarationSyntax != null &&
				propertyDeclarationSyntax.Type is GenericNameSyntax propertyGenericName &&
				propertyGenericName.Identifier.ValueText == originalObjectCreationType.Identifier.ValueText)
			{
				return propertyDeclarationSyntax.Type;
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
			else
			{
				genericNameSyntax = configuredNameSyntax as SimpleNameSyntax;
			}

			GenericNameSyntax appendedGenericName = SyntaxFactory.GenericName(genericNameSyntax.Identifier, originalTypeArguments);

			if (configuredNameSyntax is QualifiedNameSyntax qualifiedConfiguredName)
			{
				return qualifiedConfiguredName.WithRight(appendedGenericName);
			}

			return appendedGenericName;
		}
	}
}
