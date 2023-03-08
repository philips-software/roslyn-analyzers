// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestHasTimeoutCodeFixProvider)), Shared]
	public class TestHasTimeoutCodeFixProvider : SingleDiagnosticCodeFixProvider<MethodDeclarationSyntax>
	{
		protected override string Title => "Add Test Timeout";

		protected override DiagnosticId DiagnosticId => DiagnosticId.TestHasTimeoutAttribute;

		protected override async Task<Document> ApplyFix(Document document, MethodDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			// any timeout.  1000ms should be a good default.
			var defaultTimeout = properties.GetValueOrDefault(TestHasTimeoutAnalyzer.DefaultTimeoutKey) ?? "1000";

			ExpressionSyntax expression;
			if (int.TryParse(defaultTimeout, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerTimeout))
			{
				expression = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(integerTimeout));
			}
			else
			{
				expression = SyntaxFactory.ParseExpression(defaultTimeout);
			}

			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			NameSyntax name = SyntaxFactory.ParseName("Timeout");
			AttributeSyntax newAttribute = SyntaxFactory.Attribute(name,
				SyntaxFactory.AttributeArgumentList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.AttributeArgument(expression))));

			SyntaxList<AttributeListSyntax> attributeLists = node.AttributeLists;

			foreach (AttributeListSyntax attributes in attributeLists)
			{
				AttributeSyntax attributeSyntax =
					attributes.Attributes.FirstOrDefault(attr => attr.Name.ToString().Contains(@"Timeout"));
				if (attributeSyntax != null)
				{
					SyntaxNode newRoot = rootNode.ReplaceNode(attributeSyntax, newAttribute);

					return document.WithSyntaxRoot(newRoot);
				}
			}

			AttributeListSyntax attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(newAttribute));

			SyntaxList<AttributeListSyntax> newAttributeLists = node.AttributeLists.Add(attributeList);
			MethodDeclarationSyntax newMethod = node.WithAttributeLists(newAttributeLists);

			SyntaxNode root = rootNode.ReplaceNode(node, newMethod);
			Document newDocument = document.WithSyntaxRoot(root);
			return newDocument;
		}
	}
}
