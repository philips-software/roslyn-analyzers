// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidToStringOnStringCodeFixProvider)), Shared]
	public class AvoidToStringOnStringCodeFixProvider : SingleDiagnosticCodeFixProvider<InvocationExpressionSyntax>
	{
		protected override string Title => "Remove redundant ToString() call";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidToStringOnString;

		protected override async Task<Document> ApplyFix(Document document, InvocationExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			// Get the member access expression (e.g., "str.ToString")
			if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
			{
				return document;
			}

			// Replace the entire invocation with just the expression part
			ExpressionSyntax replacement = memberAccess.Expression;

			// Preserve trivia from the original node
			SyntaxTriviaList trivia = node.GetLeadingTrivia();
			ExpressionSyntax newExpressionWithLeadingTrivia = replacement.WithLeadingTrivia(trivia).WithAdditionalAnnotations(Formatter.Annotation);
			rootNode = rootNode.ReplaceNode(node, newExpressionWithLeadingTrivia);
			return document.WithSyntaxRoot(rootNode);
		}
	}
}
