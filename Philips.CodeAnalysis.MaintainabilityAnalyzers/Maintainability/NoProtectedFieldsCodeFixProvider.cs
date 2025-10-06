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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NoProtectedFieldsCodeFixProvider)), Shared]
	public class NoProtectedFieldsCodeFixProvider : SingleDiagnosticCodeFixProvider<FieldDeclarationSyntax>
	{
		protected override string Title => "Convert to protected property with private setter";

		protected override DiagnosticId DiagnosticId => DiagnosticId.NoProtectedFields;

		protected override async Task<Document> ApplyFix(Document document, FieldDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			// For now, handle single field declarations only (most common case)
			// Multiple field declarations like "protected int _a, _b;" are less common
			if (node.Declaration.Variables.Count == 1)
			{
				VariableDeclaratorSyntax variable = node.Declaration.Variables[0];
				PropertyDeclarationSyntax newProperty = CreateProperty(node, variable);
				rootNode = rootNode.ReplaceNode(node, newProperty.WithAdditionalAnnotations(Formatter.Annotation));
			}
			else
			{
				// For multiple variables, split into separate field declarations first
				// This handles cases like "protected int _a, _b;" by converting to separate properties
				var newProperties = node.Declaration.Variables
					.Select(variable => CreateProperty(node, variable))
					.ToList();

				// Replace with the first property and insert others after it
				SyntaxNode parent = node.Parent;
				var parentMembers = parent.ChildNodes().ToList();
				var nodeIndex = parentMembers.IndexOf(node);

				// Create new list of members with properties replacing the field
				var newMembers = parentMembers.Take(nodeIndex)
					.Concat(newProperties.Cast<SyntaxNode>())
					.Concat(parentMembers.Skip(nodeIndex + 1))
					.ToList();

				// Replace parent with new member list
				if (parent is ClassDeclarationSyntax classDecl)
				{
					ClassDeclarationSyntax newClass = classDecl.WithMembers(SyntaxFactory.List(newMembers.Cast<MemberDeclarationSyntax>()));
					rootNode = rootNode.ReplaceNode(parent, newClass);
				}
				else if (parent is StructDeclarationSyntax structDecl)
				{
					StructDeclarationSyntax newStruct = structDecl.WithMembers(SyntaxFactory.List(newMembers.Cast<MemberDeclarationSyntax>()));
					rootNode = rootNode.ReplaceNode(parent, newStruct);
				}
			}

			return document.WithSyntaxRoot(rootNode);
		}

		private PropertyDeclarationSyntax CreateProperty(FieldDeclarationSyntax fieldDeclaration, VariableDeclaratorSyntax variable)
		{
			// Get the field name and convert to property name
			var fieldName = variable.Identifier.ValueText;
			var propertyName = ConvertFieldNameToPropertyName(fieldName);

			// Create the property declaration
			PropertyDeclarationSyntax property = SyntaxFactory.PropertyDeclaration(fieldDeclaration.Declaration.Type, propertyName)
				.WithModifiers(fieldDeclaration.Modifiers)
				.WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
				{
					SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
						.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
					SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
						.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
						.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
				})))
				.WithLeadingTrivia(fieldDeclaration.GetLeadingTrivia())
				.WithTrailingTrivia(fieldDeclaration.GetTrailingTrivia());

			return property;
		}

		private string ConvertFieldNameToPropertyName(string fieldName)
		{
			// Remove leading underscore if present
			if (fieldName.StartsWith("_") && fieldName.Length > 1)
			{
				fieldName = fieldName.Substring(1);
			}

			// Capitalize first letter
			if (fieldName.Length > 0)
			{
				fieldName = char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
			}

			return fieldName;
		}
	}
}