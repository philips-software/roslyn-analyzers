// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestContextCodeFixProvider)), Shared]
	public class TestContextCodeFixProvider : SingleDiagnosticCodeFixProvider<SyntaxNode>
	{
		protected override string Title => "Remove Test Context declaration";

		protected override DiagnosticId DiagnosticId => DiagnosticId.TestContext;

		protected override async Task<Document> ApplyFix(Document document, SyntaxNode node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			if (rootNode == null)
			{
				return document;
			}

			// find the underlying variable
			IEnumerable<SyntaxNode> propNodes = node.DescendantNodes();
			ReturnStatementSyntax returnStatement = propNodes.OfType<ReturnStatementSyntax>().First();
			var varName = string.Empty;
			if (returnStatement?.Expression is IdentifierNameSyntax returnVar)
			{
				varName = returnVar.Identifier.ToString();
			}

			// remove the property
			rootNode = rootNode.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);

			if (!string.IsNullOrEmpty(varName))
			{
				foreach (VariableDeclarationSyntax varDeclaration in rootNode.DescendantNodes()
					.OfType<VariableDeclarationSyntax>())
				{
					if (varDeclaration.Variables[0].Identifier.ToString() == varName)
					{
						// remove the underlying variable
						if (varDeclaration.Parent != null)
						{
							rootNode = rootNode.RemoveNode(varDeclaration.Parent, SyntaxRemoveOptions.KeepNoTrivia);
						}

						break;
					}
				}
			}

			Document newDocument = document.WithSyntaxRoot(rootNode);
			return newDocument;
		}
	}
}
