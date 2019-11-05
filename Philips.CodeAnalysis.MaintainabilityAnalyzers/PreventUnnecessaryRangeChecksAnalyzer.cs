// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreventUnnecessaryRangeChecksAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Unnecessary Range Checks";
		public const string MessageFormat = @"Do not check the length of a list/array before iterating over it";
		private const string Description = @"";
		private const string Category = Categories.Readability;

		public DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.PreventUncessaryRangeChecks), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public PreventUnnecessaryRangeChecksAnalyzer()
		{ }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
		}

		private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
		{
			IfStatementSyntax ifStatementSyntax = (IfStatementSyntax)context.Node;

			if (ifStatementSyntax.Else != null)
			{
				return;
			}

			if (ifStatementSyntax.Condition == null)
			{
				return;
			}

			if (!TryFindForeach(ifStatementSyntax, out ForEachStatementSyntax forEachStatementSyntax))
			{
				return;
			}

			if (!(forEachStatementSyntax.Expression is IdentifierNameSyntax identifier))
			{
				return;
			}

			if (!IsCountGreaterThanZero(ifStatementSyntax.Condition, identifier))
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, ifStatementSyntax.IfKeyword.GetLocation()));
		}

		private bool TryFindForeach(IfStatementSyntax ifStatementSyntax, out ForEachStatementSyntax forEachStatementSyntax)
		{
			forEachStatementSyntax = null;

			if (!(ifStatementSyntax.Statement is BlockSyntax block))
			{
				forEachStatementSyntax = ifStatementSyntax.Statement as ForEachStatementSyntax;

				return forEachStatementSyntax != null;
			}

			if (block.Statements.Count != 1)
			{
				return false;
			}

			forEachStatementSyntax = block.Statements[0] as ForEachStatementSyntax;

			if (forEachStatementSyntax == null)
			{
				return false;
			}

			return true;
		}

		private bool IsCountGreaterThanZero(ExpressionSyntax condition, IdentifierNameSyntax identifier)
		{
			if (condition is ParenthesizedExpressionSyntax parenthesized)
			{
				return IsCountGreaterThanZero(parenthesized.Expression, identifier);
			}

			if (condition is BinaryExpressionSyntax binaryExpressionSyntax)
			{
				return IsCountGreaterThanZero(binaryExpressionSyntax, identifier);
			}

			return false;
		}
		private bool IsCountGreaterThanZero(BinaryExpressionSyntax condition, IdentifierNameSyntax identifier)
		{
			switch (condition.OperatorToken.Kind())
			{
				case SyntaxKind.GreaterThanToken:
				case SyntaxKind.ExclamationEqualsToken:
					break;
				default:
					return false;
			}

			if (!(condition.Right is LiteralExpressionSyntax literal))
			{
				return false;
			}

			if (literal.Token.Kind() != SyntaxKind.NumericLiteralToken)
			{
				return false;
			}

			if (literal.Token.ValueText != "0")
			{
				return false;
			}

			if (!TryGetIdentifiers(condition.Left, out IdentifierNameSyntax ifIdentifier, out IdentifierNameSyntax method))
			{
				return false;
			}

			switch (method.Identifier.Text)
			{
				case "Length":
				case "Count":
				case "Count()":
					break;
				default:
					return false;
			}

			if (ifIdentifier.Identifier.Text != identifier.Identifier.Text)
			{
				return false;
			}


			return true;
		}

		private bool TryGetIdentifiers(ExpressionSyntax expression, out IdentifierNameSyntax ifIdentifier, out IdentifierNameSyntax method)
		{
			ifIdentifier = null;
			method = null;

			if (expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
			{
				return TryGetIdentifiers(memberAccessExpressionSyntax, out ifIdentifier, out method);
			}

			if (expression is InvocationExpressionSyntax invocationExpression)
			{
				return TryGetIdentifiers(invocationExpression.Expression, out ifIdentifier, out method);
			}

			return false;
		}

		private bool TryGetIdentifiers(MemberAccessExpressionSyntax expression, out IdentifierNameSyntax ifIdentifier, out IdentifierNameSyntax method)
		{
			ifIdentifier = null;
			method = null;

			ifIdentifier = expression.Expression as IdentifierNameSyntax;
			if (ifIdentifier == null)
			{
				return false;
			}

			method = expression.Name as IdentifierNameSyntax;

			if (method == null)
			{
				return false;
			}

			return true;
		}
	}
}
