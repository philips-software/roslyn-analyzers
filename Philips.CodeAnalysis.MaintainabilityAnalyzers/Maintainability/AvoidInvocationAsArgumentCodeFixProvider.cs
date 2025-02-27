// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidInvocationAsArgumentCodeFixProvider)), Shared]
	public class AvoidInvocationAsArgumentCodeFixProvider : SingleDiagnosticCodeFixProvider<ExpressionSyntax>
	{
		protected override string Title => "Extract local variable";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidInvocationAsArgument;

		protected override ExpressionSyntax GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			// Find the nested method call identified by the diagnostic.
			SyntaxNode node = root.FindNode(diagnosticSpan, false, true);
			var expressionNode = node as ExpressionSyntax;
			if (expressionNode == null && node is ArgumentSyntax argumentNode)
			{
				expressionNode = argumentNode.Expression;
			}

			// Make sure it's part of a statement that we know how to handle.
			// For example, we do not currently handle method expressions (ie =>)
			StatementSyntax fullExistingExpressionSyntax = expressionNode?.FirstAncestorOrSelf<ExpressionStatementSyntax>();
			fullExistingExpressionSyntax ??= expressionNode?.FirstAncestorOrSelf<ReturnStatementSyntax>();
			fullExistingExpressionSyntax ??= expressionNode?.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
			fullExistingExpressionSyntax ??= expressionNode?.FirstAncestorOrSelf<IfStatementSyntax>();
			fullExistingExpressionSyntax ??= expressionNode?.FirstAncestorOrSelf<WhileStatementSyntax>();

			return fullExistingExpressionSyntax == null ? null : expressionNode;
		}

		protected override async Task<Document> ApplyFix(Document document, ExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			ExpressionSyntax argumentSyntax = node;

			string newName;
			SyntaxToken identifier;
			IOperation operation = semanticModel.GetOperation(argumentSyntax, cancellationToken);
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
			VariableDeclaratorSyntax variableDeclarator = SyntaxFactory.VariableDeclarator(identifier).WithInitializer(SyntaxFactory.EqualsValueClause(argumentSyntax));
			VariableDeclarationSyntax variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("var")).AddVariables(variableDeclarator);
			LocalDeclarationStatementSyntax localDeclarationStatementSyntax = SyntaxFactory.LocalDeclarationStatement(variableDeclaration);


			// Get the whole statement that contains the violation
			StatementSyntax fullExistingExpressionSyntax = argumentSyntax.FirstAncestorOrSelf<ExpressionStatementSyntax>();
			fullExistingExpressionSyntax ??= argumentSyntax.FirstAncestorOrSelf<ReturnStatementSyntax>();
			fullExistingExpressionSyntax ??= argumentSyntax.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
			fullExistingExpressionSyntax ??= argumentSyntax.FirstAncestorOrSelf<IfStatementSyntax>();
			fullExistingExpressionSyntax ??= argumentSyntax.FirstAncestorOrSelf<WhileStatementSyntax>();

			// Replace the violation with "renameMe"
			IdentifierNameSyntax identifierSyntax = SyntaxFactory.IdentifierName(newName);
			StatementSyntax newFullExistingExpressionSyntax = fullExistingExpressionSyntax.ReplaceNode(argumentSyntax, identifierSyntax);

			// Move all the leading trivia from the existing statement to our new statement
			SyntaxTriviaList existingLeadingTrivia = fullExistingExpressionSyntax.GetLeadingTrivia();
			LocalDeclarationStatementSyntax formattedLocalDeclarationSyntax = localDeclarationStatementSyntax.WithLeadingTrivia(existingLeadingTrivia);

			// Put just the leading whitespace back into the original statement (ie, remove comments on the previous line - we put them on our new first statement instead)
			newFullExistingExpressionSyntax = newFullExistingExpressionSyntax.WithoutLeadingTrivia();
			if (existingLeadingTrivia.Count > 0)
			{
				newFullExistingExpressionSyntax = newFullExistingExpressionSyntax.WithLeadingTrivia(existingLeadingTrivia[existingLeadingTrivia.Count - 1]);
			}

			// If we're not already inside a block statement, we need to make it so.
			if (fullExistingExpressionSyntax.Parent is StatementSyntax and not BlockSyntax)
			{
				BlockSyntax blockSyntax = SyntaxFactory.Block(formattedLocalDeclarationSyntax, newFullExistingExpressionSyntax);
				rootNode = rootNode.ReplaceNode(fullExistingExpressionSyntax, blockSyntax);
			}
			else
			{
				List<SyntaxNode> newNodes = [formattedLocalDeclarationSyntax, newFullExistingExpressionSyntax];
				rootNode = rootNode.ReplaceNode(fullExistingExpressionSyntax, newNodes);
			}

			return document.WithSyntaxRoot(rootNode);
		}

		private static string NiceVariableName(ExpressionSyntax argumentSyntax)
		{
			var niceName = @"renameMe";
			if (argumentSyntax is InvocationExpressionSyntax invocationExpressionSyntax)
			{
				var newNameSuffix = invocationExpressionSyntax.Expression.GetText().ToString();
				var indexOfDot = newNameSuffix.LastIndexOf('.');
				if (indexOfDot != -1)
				{
					newNameSuffix = newNameSuffix.Substring(indexOfDot + 1);
				}

				newNameSuffix = newNameSuffix[0].ToString().ToUpperInvariant() +
								newNameSuffix.Substring(1);
				niceName = @"resultOf" + newNameSuffix;
			}

			return niceName;
		}
	}
}
