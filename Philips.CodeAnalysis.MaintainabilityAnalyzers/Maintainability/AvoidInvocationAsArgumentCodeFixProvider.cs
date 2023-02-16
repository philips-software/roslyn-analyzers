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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidInvocationAsArgumentCodeFixProvider)), Shared]
	public class AvoidInvocationAsArgumentCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Extract local variable";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.AvoidInvocationAsArgument));
		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the nested method call identified by the diagnostic.
			ExpressionSyntax node = root.FindNode(diagnosticSpan, false, true) as ExpressionSyntax;

			// Make sure it's part of a statement that we know how to handle.
			// For example, we do not currently handle method expressions (ie =>)
			StatementSyntax fullExistingExpressionSyntax = node?.FirstAncestorOrSelf<ExpressionStatementSyntax>();
			fullExistingExpressionSyntax ??= node.FirstAncestorOrSelf<ReturnStatementSyntax>();
			fullExistingExpressionSyntax ??= node.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
			fullExistingExpressionSyntax ??= node.FirstAncestorOrSelf<IfStatementSyntax>();
			fullExistingExpressionSyntax ??= node.FirstAncestorOrSelf<WhileStatementSyntax>();

			if (fullExistingExpressionSyntax != null)
			{
				// Register a code action that will invoke the fix.
				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => ExtractLocalVariable(context.Document, node, c),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> ExtractLocalVariable(Document document, ExpressionSyntax argumentSyntax, CancellationToken c)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

			var semanticModel = await document.GetSemanticModelAsync(c).ConfigureAwait(false);

			string newName;
			SyntaxToken identifier;
			var operation = semanticModel.GetOperation(argumentSyntax, c);
			if (operation?.Parent is IArgumentOperation argumentOperation)
			{
				IParameterSymbol parameterSymbol = argumentOperation.Parameter;
				newName = parameterSymbol.Name;
				identifier = SyntaxFactory.Identifier(newName).WithAdditionalAnnotations(RenameAnnotation.Create());
				if (identifier.Text == StringConstants.Value)
				{
					newName = NiceVariableName(argumentSyntax);
					identifier = SyntaxFactory.Identifier(newName);
				}
			}
			else
			{
				newName = NiceVariableName(argumentSyntax);
				identifier = SyntaxFactory.Identifier(newName).WithAdditionalAnnotations(RenameAnnotation.Create());
			}

			// Build "var renameMe = [blah]"
			var variableDeclarator = SyntaxFactory.VariableDeclarator(identifier).WithInitializer(SyntaxFactory.EqualsValueClause(argumentSyntax));
			var variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("var")).AddVariables(variableDeclarator);
			LocalDeclarationStatementSyntax localDeclarationStatementSyntax = SyntaxFactory.LocalDeclarationStatement(variableDeclaration);


			// Get the whole statement that contains the violation
			StatementSyntax fullExistingExpressionSyntax = argumentSyntax.FirstAncestorOrSelf<ExpressionStatementSyntax>();
			fullExistingExpressionSyntax ??= argumentSyntax.FirstAncestorOrSelf<ReturnStatementSyntax>();
			fullExistingExpressionSyntax ??= argumentSyntax.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
			fullExistingExpressionSyntax ??= argumentSyntax.FirstAncestorOrSelf<IfStatementSyntax>();
			fullExistingExpressionSyntax ??= argumentSyntax.FirstAncestorOrSelf<WhileStatementSyntax>();

			// Replace the violation with "renameMe"
			IdentifierNameSyntax identifierSyntax = SyntaxFactory.IdentifierName(newName);
			var newFullExistingExpressionSyntax = fullExistingExpressionSyntax.ReplaceNode(argumentSyntax, identifierSyntax);

			// Move all the leading trivia from the existing statement to our new statement
			SyntaxTriviaList existingLeadingTrivia = fullExistingExpressionSyntax.GetLeadingTrivia();
			var formattedLocalDeclarationSyntax = localDeclarationStatementSyntax.WithLeadingTrivia(existingLeadingTrivia);

			// Put just the leading whitespace back into the original statement (ie, remove comments on the previous line - we put them on our new first statement instead)
			newFullExistingExpressionSyntax = newFullExistingExpressionSyntax.WithoutLeadingTrivia();
			if (existingLeadingTrivia.Count > 0)
			{
				newFullExistingExpressionSyntax = newFullExistingExpressionSyntax.WithLeadingTrivia(existingLeadingTrivia[existingLeadingTrivia.Count - 1]);
			}

			// If we're not already inside a block statement, we need to make it so.
			// (E.g., "if (true) Foo(Moo())" => "if (true) { var renameMe=Moo();Foo(renameMe); }"
			if (fullExistingExpressionSyntax.Parent is StatementSyntax and not BlockSyntax)
			{
				BlockSyntax blockSyntax = SyntaxFactory.Block(formattedLocalDeclarationSyntax, newFullExistingExpressionSyntax);
				rootNode = rootNode.ReplaceNode(fullExistingExpressionSyntax, blockSyntax);
			}
			else
			{
				List<SyntaxNode> newNodes = new() { formattedLocalDeclarationSyntax, newFullExistingExpressionSyntax };
				rootNode = rootNode.ReplaceNode(fullExistingExpressionSyntax, newNodes);
			}

			return document.WithSyntaxRoot(rootNode);
		}

		private static string NiceVariableName(ExpressionSyntax argumentSyntax)
		{
			string niceName = @"renameMe";
			if (argumentSyntax is InvocationExpressionSyntax invocationExpressionSyntax)
			{
				string newNameSuffix = invocationExpressionSyntax.Expression.GetText().ToString();
				int indexOfDot = newNameSuffix.LastIndexOf('.');
				if (indexOfDot != -1)
				{
					newNameSuffix = newNameSuffix.Substring(indexOfDot + 1, newNameSuffix.Length - indexOfDot - 1);
				}

				newNameSuffix = newNameSuffix[0].ToString().ToUpperInvariant() +
								newNameSuffix.Substring(1, newNameSuffix.Length - 1);
				niceName = @"resultOf" + newNameSuffix;
			}

			return niceName;
		}
	}
}
