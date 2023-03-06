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

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestHasCategoryCodeFixProvider)), Shared]
	public class TestHasCategoryCodeFixProvider : SingleDiagnosticCodeFixProvider<MethodDeclarationSyntax>
	{
		protected override string Title => "Add Test Category";

		protected override DiagnosticId DiagnosticId => DiagnosticId.TestHasCategoryAttribute;

		protected override async Task<Document> ApplyFix(Document document, MethodDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxList<AttributeListSyntax> attributeLists = node.AttributeLists;

			if (attributeLists.Any(list =>
					list.Attributes.Any(attributeSyntax => attributeSyntax.Name.ToString().Contains(@"TestCategory"))))
			{
				return document;
			}

			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			NameSyntax name = SyntaxFactory.ParseName("TestCategory");
			AttributeArgumentListSyntax arguments = SyntaxFactory.ParseAttributeArgumentList("(TestDefinitions.UnitTests)");
			AttributeSyntax attribute = SyntaxFactory.Attribute(name, arguments);

			AttributeListSyntax attributeList = SyntaxFactory.AttributeList(
				SyntaxFactory.SingletonSeparatedList(attribute));

			attributeLists = node.AttributeLists.Add(attributeList);
			MethodDeclarationSyntax newMethod = node.WithAttributeLists(attributeLists);

			SyntaxNode newRoot = rootNode.ReplaceNode(node, newMethod);
			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}
