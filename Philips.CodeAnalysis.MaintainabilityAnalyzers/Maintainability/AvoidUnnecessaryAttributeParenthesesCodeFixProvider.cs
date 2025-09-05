// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

#pragma warning disable IDE0055 // Fix formatting

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidUnnecessaryAttributeParenthesesCodeFixProvider)),
		Shared]
	public class AvoidUnnecessaryAttributeParenthesesCodeFixProvider : SingleDiagnosticCodeFixProvider<AttributeSyntax>
	{
		protected override string Title => "Remove unnecessary parentheses from attribute";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidUnnecessaryAttributeParentheses;

		protected override async Task<Document> ApplyFix(Document document, AttributeSyntax node,
			ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			// Create a new attribute without the argument list
			AttributeSyntax newAttribute = node.WithArgumentList(null);

			// Replace the old attribute with the new one
			root = root.ReplaceNode(node, newAttribute);

			return document.WithSyntaxRoot(root);
		}
	}
}
