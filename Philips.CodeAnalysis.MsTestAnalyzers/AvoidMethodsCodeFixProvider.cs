// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidMethodsCodeFixProvider)), Shared]
	public class AvoidMethodsCodeFixProvider : SingleDiagnosticCodeFixProvider<MethodDeclarationSyntax>
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds =>
			ImmutableArray.Create(DiagnosticId.AvoidTestInitializeMethod.ToId(),
				DiagnosticId.AvoidClassInitializeMethod.ToId(),
				DiagnosticId.AvoidClassCleanupMethod.ToId(),
				DiagnosticId.AvoidTestCleanupMethod.ToId());

		protected override string Title => "Remove this Method";

		protected override DiagnosticId DiagnosticId { get; }

		protected override async Task<Document> ApplyFix(Document document, MethodDeclarationSyntax node, ImmutableDictionary<string, string> properties,
			CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode newRoot = rootNode.RemoveNode(node, SyntaxRemoveOptions.KeepDirectives);

			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}
