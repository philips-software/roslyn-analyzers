// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidTaskResultCodeFixProvider)), Shared]
	public class AvoidTaskResultCodeFixProvider : SingleDiagnosticCodeFixProvider<MemberAccessExpressionSyntax>
	{
		protected override string Title => "Use await";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidTaskResult;

		protected override async Task<Document> ApplyFix(Document document, MemberAccessExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxTriviaList trivia = node.GetLeadingTrivia();
			AwaitExpressionSyntax newExpression = SyntaxFactory.AwaitExpression(node.Expression);
			AwaitExpressionSyntax newExpressionWithLeadingTrivia = newExpression.WithLeadingTrivia(trivia).WithAdditionalAnnotations(Formatter.Annotation);
			rootNode = rootNode.ReplaceNode(node, newExpressionWithLeadingTrivia);
			return document.WithSyntaxRoot(rootNode);
		}
	}
}
