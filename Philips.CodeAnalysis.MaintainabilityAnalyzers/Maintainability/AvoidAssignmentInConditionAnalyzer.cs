// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Diagnostic for variable assignment inside an if condition.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAssignmentInConditionAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = "Assignment in condition.";
		private const string MessageFormat = Title;
		private const string Description = Title;

		public AvoidAssignmentInConditionAnalyzer()
			: base(DiagnosticId.AvoidAssignmentInCondition, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
			context.RegisterSyntaxNodeAction(AnalyzeTernary, SyntaxKind.ConditionalExpression);
		}

		private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			ExpressionSyntax ifStatement = ((IfStatementSyntax)context.Node).Condition;
			CheckDescendantHasNoAssignment(context, ifStatement);
		}

		private void AnalyzeTernary(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			ExpressionSyntax condition = ((ConditionalExpressionSyntax)context.Node).Condition;
			CheckDescendantHasNoAssignment(context, condition);
		}

		private void CheckDescendantHasNoAssignment(SyntaxNodeAnalysisContext context, SyntaxNode node)
		{
			if (
				node.DescendantTokens().Any(child => child.IsKind(SyntaxKind.EqualsToken)) &&
				!node.DescendantNodes().Any(node => node.IsKind(SyntaxKind.AnonymousObjectCreationExpression)))
			{
				Location location = node.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location));
			}
		}
	}
}
