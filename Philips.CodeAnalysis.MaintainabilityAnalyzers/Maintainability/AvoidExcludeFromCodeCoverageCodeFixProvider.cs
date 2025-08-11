// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidExcludeFromCodeCoverageCodeFixProvider)), Shared]
	public class AvoidExcludeFromCodeCoverageCodeFixProvider : SingleDiagnosticCodeFixProvider<AttributeListSyntax>
	{
		private const string ExcludeFromCodeCoverageAttributeTypeName = "ExcludeFromCodeCoverage";

		protected override string Title => "Remove ExcludeFromCodeCoverage attribute";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidExcludeFromCodeCoverage;

		protected override async Task<Document> ApplyFix(Document document, AttributeListSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			NamespaceResolver aliases = Helper.ForNamespaces.GetUsingAliases(node);

			AttributeSyntax[] attributesToRemove = node.Attributes
				.Where(attr => IsExcludeFromCodeCoverage(aliases, attr))
				.ToArray();

			if (attributesToRemove.Length == 0)
			{
				return document;
			}

			SyntaxNode newRoot;
			if (attributesToRemove.Length == node.Attributes.Count)
			{
				newRoot = rootNode.RemoveNode(node, SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepExteriorTrivia);
			}
			else
			{
				AttributeListSyntax newAttributeList = node.RemoveNodes(attributesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
				newRoot = rootNode.ReplaceNode(node, newAttributeList);
			}

			return document.WithSyntaxRoot(newRoot);
		}

		private bool IsExcludeFromCodeCoverage(NamespaceResolver aliases, AttributeSyntax attribute)
		{
			return aliases.GetDealiasedName(attribute.Name).Contains(ExcludeFromCodeCoverageAttributeTypeName);
		}
	}
}
