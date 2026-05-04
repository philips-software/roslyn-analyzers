// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MoqAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MockDisposableClassesShouldSetupDisposeCodeFixProvider)), Shared]
	public class MockDisposableClassesShouldSetupDisposeCodeFixProvider : CodeFixProvider
	{
		private const string PreferredTypeTitle = "Use configured disposable mock type";
		private const string ProtectedSetupTitle = "Insert Protected().Setup Dispose CallBase (local variables only; field/property initializers are not modified)";
		private const string MoqProtectedNamespace = "Moq.Protected";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.MockDisposableObjectsShouldSetupDispose.ToId());

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			if (root == null)
			{
				return;
			}

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			SyntaxToken token = root.FindToken(diagnosticSpan.Start);
			ExpressionSyntax node = token.Parent?.AncestorsAndSelf().OfType<ExpressionSyntax>()
				.FirstOrDefault(e => e is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax);

			if (node == null)
			{
				return;
			}

			var configuredTypeName = GetPreferredDisposableMockType(diagnostic.Properties);

			if (!string.IsNullOrWhiteSpace(configuredTypeName))
			{
				context.RegisterCodeFix(
					CodeAction.Create(
						title: PreferredTypeTitle,
						createChangedDocument: c => ApplyPreferredTypeFix(context.Document, node, configuredTypeName, c),
						equivalenceKey: PreferredTypeTitle),
					diagnostic);
			}

			// Always offer the Protected().Setup("Dispose", ...).CallBase() alternative.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: ProtectedSetupTitle,
					createChangedDocument: c => ApplyProtectedSetupFix(context.Document, node, c),
					equivalenceKey: ProtectedSetupTitle),
				diagnostic);
		}

		private static async Task<Document> ApplyPreferredTypeFix(Document document, ExpressionSyntax node, string configuredTypeName, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (rootNode == null)
			{
				return document;
			}

			ExpressionSyntax currentNode = FindCurrentNode<ExpressionSyntax>(rootNode, node);
			if (currentNode == null)
			{
				return document;
			}

			TypeSyntax declaredTypeToReplace = GetDeclaredTypeFromContext(currentNode);

			if (currentNode is ImplicitObjectCreationExpressionSyntax)
			{
				if (declaredTypeToReplace is not GenericNameSyntax declaredGenericName)
				{
					return document;
				}

				TypeSyntax replacementType = CreateReplacementTypeSyntax(configuredTypeName, declaredGenericName.TypeArgumentList)
					.WithAdditionalAnnotations(Formatter.Annotation);

				SyntaxNode newRoot = rootNode.ReplaceNode(
					declaredTypeToReplace,
					replacementType.WithTriviaFrom(declaredTypeToReplace));

				return document.WithSyntaxRoot(newRoot);
			}

			if (currentNode is ObjectCreationExpressionSyntax explicitObjectCreation &&
				explicitObjectCreation.Type is GenericNameSyntax explicitGenericName)
			{
				TypeSyntax replacementType = CreateReplacementTypeSyntax(configuredTypeName, explicitGenericName.TypeArgumentList)
					.WithAdditionalAnnotations(Formatter.Annotation);

				SyntaxNode newRoot;
				if (IsMatchingMockDeclaredType(declaredTypeToReplace, explicitGenericName))
				{
					newRoot = rootNode.ReplaceNodes(
						new SyntaxNode[] { explicitObjectCreation.Type, declaredTypeToReplace },
						(original, _) => replacementType.WithTriviaFrom(original));
				}
				else
				{
					newRoot = rootNode.ReplaceNode(
						explicitObjectCreation.Type,
						replacementType.WithTriviaFrom(explicitObjectCreation.Type));
				}

				return document.WithSyntaxRoot(newRoot);
			}

			return document;
		}

		private static async Task<Document> ApplyProtectedSetupFix(Document document, ExpressionSyntax node, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (rootNode == null)
			{
				return document;
			}

			ExpressionSyntax currentNode = FindCurrentNode<ExpressionSyntax>(rootNode, node);
			if (currentNode == null)
			{
				return document;
			}

			// Identify the mock identifier from either local variable declaration or assignment.
			(string mockIdentifier, StatementSyntax containingStatement) result = GetMockIdentifierAndStatement(currentNode);
			var mockIdentifier = result.mockIdentifier;
			StatementSyntax containingStatement = result.containingStatement;
			if (mockIdentifier == null || containingStatement == null)
			{
				// Skip field/property initializers: too complex to safely insert in a constructor/TestInitialize.
				return document;
			}

			StatementSyntax setupStatement = BuildProtectedSetupStatement(mockIdentifier)
				.WithAdditionalAnnotations(Formatter.Annotation);

			if (containingStatement.Parent is not BlockSyntax block)
			{
				return document;
			}

			SyntaxList<StatementSyntax> statements = block.Statements;
			var index = statements.IndexOf(containingStatement);
			if (index < 0)
			{
				return document;
			}

			SyntaxList<StatementSyntax> newStatements = statements.Insert(index + 1, setupStatement);
			BlockSyntax newBlock = block.WithStatements(newStatements);

			SyntaxNode newRoot = rootNode.ReplaceNode(block, newBlock);

			// Ensure Moq.Protected using directive is present for ItExpr.
			newRoot = EnsureUsingDirective(newRoot, MoqProtectedNamespace);

			return document.WithSyntaxRoot(newRoot);
		}

		private static (string mockIdentifier, StatementSyntax containingStatement) GetMockIdentifierAndStatement(ExpressionSyntax currentNode)
		{
			if (currentNode.Parent is EqualsValueClauseSyntax equalsValueClause &&
				equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
				variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration &&
				variableDeclaration.Parent is LocalDeclarationStatementSyntax localDeclaration)
			{
				return (variableDeclarator.Identifier.ValueText, localDeclaration);
			}

			if (currentNode.Parent is AssignmentExpressionSyntax assignment &&
				assignment.Parent is ExpressionStatementSyntax expressionStatement &&
				assignment.Left is IdentifierNameSyntax identifierName)
			{
				return (identifierName.Identifier.ValueText, expressionStatement);
			}

			if (currentNode.Parent is AssignmentExpressionSyntax assignmentMember &&
				assignmentMember.Parent is ExpressionStatementSyntax expressionStatement2 &&
				assignmentMember.Left is MemberAccessExpressionSyntax memberAccess)
			{
				return (memberAccess.ToString(), expressionStatement2);
			}

			return (null, null);
		}

		private static ExpressionStatementSyntax BuildProtectedSetupStatement(string mockIdentifier)
		{
			// mockIdentifier.Protected().Setup("Dispose", ItExpr.IsAny<bool>()).CallBase();
			ExpressionSyntax mockExpression = SyntaxFactory.ParseExpression(mockIdentifier);

			InvocationExpressionSyntax protectedInvocation = SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					mockExpression,
					SyntaxFactory.IdentifierName("Protected")));

			ArgumentListSyntax setupArgs = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
			{
				SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
					SyntaxKind.StringLiteralExpression,
					SyntaxFactory.Literal("Dispose"))),
				SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(
					SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						SyntaxFactory.IdentifierName("ItExpr"),
						SyntaxFactory.GenericName(SyntaxFactory.Identifier("IsAny"))
							.WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
								SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
									SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword))))))))
			}));

			InvocationExpressionSyntax setupInvocation = SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					protectedInvocation,
					SyntaxFactory.IdentifierName("Setup")),
				setupArgs);

			InvocationExpressionSyntax callBaseInvocation = SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					setupInvocation,
					SyntaxFactory.IdentifierName("CallBase")),
				SyntaxFactory.ArgumentList());

			return SyntaxFactory.ExpressionStatement(callBaseInvocation);
		}

		private static SyntaxNode EnsureUsingDirective(SyntaxNode root, string namespaceName)
		{
			if (root is not CompilationUnitSyntax compilationUnit)
			{
				return root;
			}

			var alreadyExists = compilationUnit.Usings.Any(u => u.Name != null && u.Name.ToString() == namespaceName);
			if (alreadyExists)
			{
				return root;
			}

			UsingDirectiveSyntax newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
				.WithAdditionalAnnotations(Formatter.Annotation);

			return compilationUnit.AddUsings(newUsing);
		}

		private static TNode FindCurrentNode<TNode>(SyntaxNode rootNode, SyntaxNode originalNode)
			where TNode : SyntaxNode
		{
			if (rootNode == null || originalNode == null)
			{
				return null;
			}

			SyntaxNode currentNode = rootNode.FindNode(originalNode.Span, getInnermostNodeForTie: true);
			return currentNode as TNode;
		}

		private static TypeSyntax GetDeclaredTypeFromContext(SyntaxNode node)
		{
			VariableDeclarationSyntax variableDeclarationSyntax = node.FirstAncestorOrSelf<VariableDeclarationSyntax>();
			if (variableDeclarationSyntax != null)
			{
				return variableDeclarationSyntax.Type;
			}

			PropertyDeclarationSyntax propertyDeclarationSyntax = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
			if (propertyDeclarationSyntax != null)
			{
				return propertyDeclarationSyntax.Type;
			}

			FieldDeclarationSyntax fieldDeclarationSyntax = node.FirstAncestorOrSelf<FieldDeclarationSyntax>();
			if (fieldDeclarationSyntax != null)
			{
				return fieldDeclarationSyntax.Declaration?.Type;
			}

			return null;
		}

		private static bool IsMatchingMockDeclaredType(TypeSyntax declaredTypeSyntax, GenericNameSyntax originalObjectCreationType)
		{
			return declaredTypeSyntax is GenericNameSyntax declaredGenericName &&
				declaredGenericName.Identifier.ValueText == originalObjectCreationType.Identifier.ValueText;
		}

		private static string GetPreferredDisposableMockType(ImmutableDictionary<string, string> properties)
		{
			if (properties != null &&
				properties.TryGetValue(MockDisposableClassesShouldSetupDisposeAnalyzer.PreferredDisposableMockTypeProperty, out var configuredTypeName) &&
				!string.IsNullOrWhiteSpace(configuredTypeName))
			{
				return configuredTypeName.Trim();
			}

			return null;
		}

		private static TypeSyntax CreateReplacementTypeSyntax(string configuredTypeName, TypeArgumentListSyntax originalTypeArguments)
		{
			NameSyntax configuredNameSyntax = SyntaxFactory.ParseName(configuredTypeName);

			SimpleNameSyntax genericNameSyntax;
			if (configuredNameSyntax is QualifiedNameSyntax qualifiedNameSyntax)
			{
				genericNameSyntax = qualifiedNameSyntax.Right;
			}
			else
			{
				genericNameSyntax = configuredNameSyntax as SimpleNameSyntax;
			}

			GenericNameSyntax appendedGenericName = SyntaxFactory.GenericName(genericNameSyntax.Identifier, originalTypeArguments);

			if (configuredNameSyntax is QualifiedNameSyntax qualifiedConfiguredName)
			{
				return qualifiedConfiguredName.WithRight(appendedGenericName);
			}

			return appendedGenericName;
		}
	}
}
