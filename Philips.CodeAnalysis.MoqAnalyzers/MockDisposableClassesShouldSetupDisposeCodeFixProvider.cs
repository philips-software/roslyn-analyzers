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

			ExpressionSyntax currentNode = FindCurrentNode<ExpressionSyntax>(rootNode, node);
			if (currentNode == null)
			{
				return document;
			}

			TypeSyntax declaredTypeToReplace = GetDeclaredTypeFromContext(currentNode);

			if (currentNode is ImplicitObjectCreationExpressionSyntax)
			{
				if (declaredTypeToReplace is not GenericNameSyntax declaredGenericName)
				{
					return document;
				}

				TypeSyntax replacementType = CreateReplacementTypeSyntax(configuredTypeName, declaredGenericName.TypeArgumentList)
					.WithAdditionalAnnotations(Formatter.Annotation);

				SyntaxNode newRoot = rootNode.ReplaceNode(
					declaredTypeToReplace,
					replacementType.WithTriviaFrom(declaredTypeToReplace));

				return document.WithSyntaxRoot(newRoot);
			}

			if (currentNode is ObjectCreationExpressionSyntax explicitObjectCreation &&
				explicitObjectCreation.Type is GenericNameSyntax explicitGenericName)
			{
				TypeSyntax replacementType = CreateReplacementTypeSyntax(configuredTypeName, explicitGenericName.TypeArgumentList)
					.WithAdditionalAnnotations(Formatter.Annotation);

				SyntaxNode newRoot;
				if (IsMatchingMockDeclaredType(declaredTypeToReplace, explicitGenericName))
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

				return document.WithSyntaxRoot(newRoot);
			}

			return document;
		}

		private static TNode FindCurrentNode<TNode>(SyntaxNode rootNode, SyntaxNode originalNode)
			where TNode : SyntaxNode
		{
			if (rootNode == null || originalNode == null)
			{
				return null;
			}

			SyntaxNode currentNode = rootNode.FindNode(originalNode.Span, getInnermostNodeForTie: true);
			return currentNode as TNode;
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

			FieldDeclarationSyntax fieldDeclarationSyntax = node.FirstAncestorOrSelf<FieldDeclarationSyntax>();
			if (fieldDeclarationSyntax != null)
			{
				return fieldDeclarationSyntax.Declaration?.Type;
			}

			return null;
		}

		private static bool IsMatchingMockDeclaredType(TypeSyntax declaredTypeSyntax, GenericNameSyntax originalObjectCreationType)
		{
			return declaredTypeSyntax is GenericNameSyntax declaredGenericName &&
				declaredGenericName.Identifier.ValueText == originalObjectCreationType.Identifier.ValueText;
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
