﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidOwnerAttributeCodeFixProvider)), Shared]
	public class AvoidOwnerAttributeCodeFixProvider : SingleDiagnosticCodeFixProvider<MethodDeclarationSyntax>
	{
		protected override string Title => "Remove Owner Attribute";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidOwnerAttribute;

		protected override async Task<Document> ApplyFix(Document document, MethodDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			var newAttributes = new SyntaxList<AttributeListSyntax>();
			foreach (AttributeListSyntax attributeList in node.AttributeLists)
			{
				AttributeSyntax[] nodesToRemove = attributeList.Attributes.Where(att => (att.Name as IdentifierNameSyntax).Identifier.Text.StartsWith("Owner")).ToArray();

				if (nodesToRemove.Length != attributeList.Attributes.Count)
				{
					AttributeListSyntax newAttribute = attributeList.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
					newAttributes = newAttributes.Add(newAttribute);
				}
			}

			MethodDeclarationSyntax newMethod = node.WithAttributeLists(newAttributes);
			SyntaxNode newRoot = rootNode.ReplaceNode(node, newMethod);
			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}
