// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocumentationCodeFixProvider)), Shared]
	public class XmlDocumentationCodeFixProvider : SingleDiagnosticCodeFixProvider<SyntaxNode>
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.EmptyXmlComments), Helper.ToDiagnosticId(DiagnosticId.XmlDocumentationShouldAddValue));

		protected override string Title => "Remove Summary";

		protected override DiagnosticId DiagnosticId { get; }

		protected override async Task<Document> ApplyFix(Document document, SyntaxNode node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode newRoot = root;
			foreach (SyntaxTrivia trivia in node.GetLeadingTrivia())
			{
				if (trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia)
				{
					newRoot = root.ReplaceTrivia(trivia, SyntaxTriviaList.Empty);

					// The formatting is slightly off after this. Rather than figure it out the hard way, just Format the whole document.
					newRoot = newRoot.WithAdditionalAnnotations(Formatter.Annotation);
				}
			}
			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}
