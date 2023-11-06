// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	/// <summary>
	/// Base class for an <see cref="DiagnosticAnalyzer"/> which uses a single <see cref="SyntaxNodeAction{SyntaxNode}"/>.
	/// </summary>
	public abstract class SingleDiagnosticAnalyzer<TNode, TSyntaxNodeAction> : SingleDiagnosticAnalyzer where TNode : SyntaxNode where TSyntaxNodeAction : SyntaxNodeAction<TNode>, new()
	{
		public string FullyQualifiedMetaDataName { get; protected set; }

		protected SingleDiagnosticAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
											DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = true)
			: base(id, title, messageFormat, description, category, severity, isEnabled)
		{ }

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			SyntaxKind syntaxKind = GetSyntaxKind();
			if (syntaxKind == SyntaxKind.None)
			{
				throw new InvalidOperationException($"Update {nameof(GetSyntaxKind)} to include the SyntaxKind associated with {typeof(TNode)}");
			}

			if (!string.IsNullOrWhiteSpace(FullyQualifiedMetaDataName) && context.Compilation.GetTypeByMetadataName(FullyQualifiedMetaDataName) == null)
			{
				return;
			}

			context.RegisterSyntaxNodeAction(StartAnalysis, syntaxKind);
		}

		private void StartAnalysis(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new(Helper);
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			TSyntaxNodeAction syntaxNodeAction = new()
			{
				Context = context,
				Node = (TNode)context.Node,
				Rule = Rule,
				Analyzer = this,
				Helper = Helper
			};
			syntaxNodeAction.Analyze();
		}

		protected virtual SyntaxKind GetSyntaxKind()
		{
			return typeof(TNode).Name switch
			{
				nameof(CompilationUnitSyntax) => SyntaxKind.CompilationUnit,
				nameof(MethodDeclarationSyntax) => SyntaxKind.MethodDeclaration,
				nameof(PropertyDeclarationSyntax) => SyntaxKind.PropertyDeclaration,
				nameof(ObjectCreationExpressionSyntax) => SyntaxKind.ObjectCreationExpression,
				nameof(QueryExpressionSyntax) => SyntaxKind.QueryExpression,
				nameof(IfStatementSyntax) => SyntaxKind.IfStatement,
				nameof(ClassDeclarationSyntax) => SyntaxKind.ClassDeclaration,
				nameof(NamespaceDeclarationSyntax) => SyntaxKind.NamespaceDeclaration,
				nameof(TupleTypeSyntax) => SyntaxKind.TupleType,
				nameof(DestructorDeclarationSyntax) => SyntaxKind.DestructorDeclaration,
				nameof(ExpressionSyntax) => SyntaxKind.AsExpression,
				nameof(FieldDeclarationSyntax) => SyntaxKind.FieldDeclaration,
				nameof(InvocationExpressionSyntax) => SyntaxKind.InvocationExpression,
				nameof(UsingStatementSyntax) => SyntaxKind.UsingStatement,
				nameof(AttributeListSyntax) => SyntaxKind.AttributeList,
				nameof(VariableDeclarationSyntax) => SyntaxKind.VariableDeclaration,
				nameof(ConstructorDeclarationSyntax) => SyntaxKind.ConstructorDeclaration,
				nameof(ArgumentSyntax) => SyntaxKind.Argument,
				nameof(LiteralExpressionSyntax) => SyntaxKind.NumericLiteralExpression,
				nameof(PragmaWarningDirectiveTriviaSyntax) => SyntaxKind.PragmaWarningDirectiveTrivia,
				nameof(MemberAccessExpressionSyntax) => SyntaxKind.SimpleMemberAccessExpression,
				nameof(ThrowStatementSyntax) => SyntaxKind.ThrowStatement,
				nameof(ConversionOperatorDeclarationSyntax) => SyntaxKind.ConversionOperatorDeclaration,
				nameof(LockStatementSyntax) => SyntaxKind.LockStatement,
				nameof(EventFieldDeclarationSyntax) => SyntaxKind.EventFieldDeclaration,
				nameof(InterfaceDeclarationSyntax) => SyntaxKind.InterfaceDeclaration,
				nameof(AccessorDeclarationSyntax) => SyntaxKind.SetAccessorDeclaration,
				nameof(CatchClauseSyntax) => SyntaxKind.CatchClause,
				_ => SyntaxKind.None,
			};
		}
	}
}
