// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidEmptyStatementBlocksCodeFixProvider)), Shared]
	public class AvoidEmptyStatementBlocksCodeFixProvider : SingleDiagnosticCodeFixProvider<EmptyStatementSyntax>
	{
		protected override string Title => "Remove empty statement";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidEmptyStatement;

		protected override async Task<Document> ApplyFix(Document document, EmptyStatementSyntax node, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			root = root.RemoveNode(node, SyntaxRemoveOptions.KeepExteriorTrivia).WithAdditionalAnnotations(Formatter.Annotation);
			return document.WithSyntaxRoot(root);
		}
	}
}
