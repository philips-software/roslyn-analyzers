// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocumentThrownExceptionsCodeFixProvider)), Shared]
	public class DocumentThrownExceptionsCodeFixProvider : SingleDiagnosticCodeFixProvider<SyntaxNode>
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.DocumentThrownExceptions), Helper.ToDiagnosticId(DiagnosticId.DocumentUnhandledExceptions));

		protected override string Title => "Document thrown exceptions";

		protected override DiagnosticId DiagnosticId { get; }

		protected override SyntaxNode GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			SyntaxNode diagnosticNode = root.FindNode(diagnosticSpan);
			return DocumentationHelper.FindAncestorThatCanHaveDocumentation(diagnosticNode);
		}

		protected override async Task<Document> ApplyFix(Document document, SyntaxNode node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode newRoot = root;
			if (!properties.TryGetValue(StringConstants.ThrownExceptionPropertyKey, out var missingExceptionTypeName))
			{
				return document;
			}
			DocumentationHelper docHelper = new(node);
			var parts = missingExceptionTypeName.Split(',');
			foreach (var part in parts)
			{
				docHelper.AddException(part);
			}
			DocumentationCommentTriviaSyntax newComment = docHelper.CreateDocumentation();

			if (docHelper.ExistingDocumentation != null)
			{
				newComment = newComment.WithAdditionalAnnotations(Formatter.Annotation);
				newRoot = root.ReplaceNode(docHelper.ExistingDocumentation, newComment);
			}

			return document.WithSyntaxRoot(newRoot);
		}
	}
}
