// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidEmptyStatementBlocksCodeFixProvider)), Shared]
	public class AvoidEmptyStatementBlocksCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Remove empty statement";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.AvoidEmptyStatement));
		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();

			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			if (root != null)
			{
				if (root.FindNode(diagnosticSpan) is not EmptyStatementSyntax node)
				{
					return;
				}

				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => RemoveEmptyStatement(context.Document, node, c),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> RemoveEmptyStatement(Document document, EmptyStatementSyntax node, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			root = root.RemoveNode(node, SyntaxRemoveOptions.KeepExteriorTrivia).WithAdditionalAnnotations(Formatter.Annotation);
			return document.WithSyntaxRoot(root);
		}
	}
}
