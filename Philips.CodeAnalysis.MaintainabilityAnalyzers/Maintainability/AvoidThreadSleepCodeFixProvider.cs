// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidThreadSleepCodeFixProvider)), Shared]
	public class AvoidThreadSleepCodeFixProvider : SingleDiagnosticCodeFixProvider<InvocationExpressionSyntax>
	{
		protected override string Title => "Remove Thread.Sleep.";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidThreadSleep;

		protected override async Task<Document> ApplyFix(Document document, InvocationExpressionSyntax node, CancellationToken cancellationToken)
		{
			SyntaxNode syntaxNodeExpression = node.Parent;
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode newRoot = rootNode.RemoveNode(syntaxNodeExpression, SyntaxRemoveOptions.KeepDirectives);

			Document newDocument = document.WithSyntaxRoot(newRoot);

			return newDocument;
		}
	}
}
