// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidIncorrectForLoopConditionCodeFixProvider)), Shared]
	public class AvoidIncorrectForLoopConditionCodeFixProvider : SingleDiagnosticCodeFixProvider<BinaryExpressionSyntax>
	{
		protected override string Title => "Use '>= 0' instead of '> 0'";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidIncorrectForLoopCondition;

		protected override async Task<Document> ApplyFix(Document document, BinaryExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			BinaryExpressionSyntax newCondition = node.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken));

			SyntaxNode newRoot = root.ReplaceNode(node, newCondition);

			return document.WithSyntaxRoot(newRoot);
		}
	}
}