// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MergeIfStatementsCodeFixProvider)), Shared]
	public class MergeIfStatementsCodeFixProvider : SingleDiagnosticCodeFixProvider<IfStatementSyntax>
	{
		protected override string Title => "Merge with outer If Statement";

		protected override DiagnosticId DiagnosticId => DiagnosticId.MergeIfStatements;

		protected override async Task<Document> ApplyFix(Document document, IfStatementSyntax node, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			SyntaxNode parent = node.Parent;
			if (parent is BlockSyntax)
			{
				parent = parent.Parent;
			}
			if (parent is IfStatementSyntax parentIfStatementSyntax)
			{
				ExpressionSyntax mergedExpression = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, parentIfStatementSyntax.Condition, node.Condition);
				IfStatementSyntax newIfStatement = SyntaxFactory.IfStatement(mergedExpression, node.Statement)
					.WithTriviaFrom(parentIfStatementSyntax)
					.WithAdditionalAnnotations(Formatter.Annotation);

				rootNode = rootNode.ReplaceNode(parentIfStatementSyntax, newIfStatement);
			}
			return document.WithSyntaxRoot(rootNode);
		}
	}
}
