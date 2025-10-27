// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataRowOrderCodeFixProvider)), Shared]
	public class DataRowOrderCodeFixProvider : SingleDiagnosticCodeFixProvider<MethodDeclarationSyntax>
	{
		protected override string Title => "Move DataRow attributes above TestMethod";

		protected override DiagnosticId DiagnosticId => DiagnosticId.DataRowOrderInTestMethod;

		protected override async Task<Document> ApplyFix(Document document, MethodDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);

			// Get the MsTest definitions to identify attributes
			var definitions = MsTestAttributeDefinitions.FromCompilation(semanticModel.Compilation);

			// Reorder the attributes
			MethodDeclarationSyntax reorderedMethod = ReorderAttributes(node, semanticModel, definitions);

			SyntaxNode newRoot = root.ReplaceNode(node, reorderedMethod);
			return document.WithSyntaxRoot(newRoot);
		}

		private static MethodDeclarationSyntax ReorderAttributes(MethodDeclarationSyntax method, SemanticModel semanticModel, MsTestAttributeDefinitions definitions)
		{
			var dataRowAttributes = new List<AttributeSyntax>();
			var testMethodAttributes = new List<AttributeSyntax>();
			var otherAttributes = new List<AttributeSyntax>();

			// Categorize all attributes
			foreach (AttributeListSyntax attributeList in method.AttributeLists)
			{
				foreach (AttributeSyntax attribute in attributeList.Attributes)
				{
					INamedTypeSymbol attributeSymbol = semanticModel.GetSymbolInfo(attribute).Symbol?.ContainingType;
					if (attributeSymbol != null)
					{
						if (SymbolEqualityComparer.Default.Equals(attributeSymbol, definitions.DataRowSymbol))
						{
							dataRowAttributes.Add(attribute);
						}
						else if (attributeSymbol.IsDerivedFrom(definitions.TestMethodSymbol) ||
								 attributeSymbol.IsDerivedFrom(definitions.DataTestMethodSymbol))
						{
							testMethodAttributes.Add(attribute);
						}
						else
						{
							otherAttributes.Add(attribute);
						}
					}
					else
					{
						otherAttributes.Add(attribute);
					}
				}
			}

			// Create new attribute lists in the correct order: DataRow, Other, TestMethod
			SyntaxList<AttributeListSyntax> newAttributeLists = SyntaxFactory.List<AttributeListSyntax>();

			// Add DataRow attributes first
			foreach (AttributeSyntax attr in dataRowAttributes)
			{
				newAttributeLists = newAttributeLists.Add(
					SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr)));
			}

			// Add other attributes (like TestCategory, Timeout, etc.)
			foreach (AttributeSyntax attr in otherAttributes)
			{
				newAttributeLists = newAttributeLists.Add(
					SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr)));
			}

			// Add TestMethod/DataTestMethod attributes last
			foreach (AttributeSyntax attr in testMethodAttributes)
			{
				newAttributeLists = newAttributeLists.Add(
					SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr)));
			}

			return method.WithAttributeLists(newAttributeLists);
		}
	}
}