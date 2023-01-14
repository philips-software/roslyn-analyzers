// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MergeIfStatementsCodeFixProvider)), Shared]
	public class MergeIfStatementsCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Merge with outer If Statement";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.MergeIfStatements));
		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			IfStatementSyntax ifStatement = root.FindNode(diagnosticSpan) as IfStatementSyntax;

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: Title,
					createChangedDocument: c => MergeIfStatements(context.Document, ifStatement, c),
					equivalenceKey: Title),
				diagnostic);
		}

		private async Task<Document> MergeIfStatements(Document document, IfStatementSyntax ifStatementSyntax, CancellationToken c)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

			var parent = ifStatementSyntax.Parent;
			if (parent is BlockSyntax)
			{
				parent = parent.Parent;
			}
			if (parent is IfStatementSyntax parentIfStatementSyntax)
			{
				ExpressionSyntax mergedExpression = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, parentIfStatementSyntax.Condition, ifStatementSyntax.Condition);
				IfStatementSyntax newIfStatement = SyntaxFactory.IfStatement(mergedExpression, ifStatementSyntax.Statement)
					.WithTriviaFrom(parentIfStatementSyntax)
					.WithAdditionalAnnotations(Formatter.Annotation);

				rootNode = rootNode.ReplaceNode(parentIfStatementSyntax, newIfStatement);
			}
			return document.WithSyntaxRoot(rootNode);
		}
	}
}
