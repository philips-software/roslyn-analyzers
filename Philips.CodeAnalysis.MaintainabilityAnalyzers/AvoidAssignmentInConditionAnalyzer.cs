// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	/// <summary>
	/// Diagnostic for variable assignment inside an if condition.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAssignmentInConditionAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = "Assignment in condition.";
		private const string Message = "Assignment within condition on line {0}.";
		private const string Description = "Assignment in condition.";
		private const string Category = "Usage";

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.AvoidAssignmentInCondition),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: Description
			);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Rule);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
			context.RegisterSyntaxNodeAction(AnalyzeTernary, SyntaxKind.ConditionalExpression);
		}

		private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context) {
			var filePath = context.Node.SyntaxTree.FilePath;
			if (Helper.IsGeneratedCode(filePath)) {
				return;
			}

			var ifStatement = ((IfStatementSyntax)context.Node).Condition;
			CheckDescendantHasNoAssignment(context, ifStatement);
		}

		private void AnalyzeTernary(SyntaxNodeAnalysisContext context)
		{
			var filePath = context.Node.SyntaxTree.FilePath;
			if (Helper.IsGeneratedCode(filePath))
			{
				return;
			}
			var condition = ((ConditionalExpressionSyntax)context.Node).Condition;
			CheckDescendantHasNoAssignment(context, condition);
		}

		private void CheckDescendantHasNoAssignment(SyntaxNodeAnalysisContext context, SyntaxNode node)
		{
			bool found = false;
			foreach (var child in node.DescendantTokens())
			{
				if (child.IsKind(SyntaxKind.EqualsToken))
				{
					found = true;
					break;
				}
			}
			if (found)
			{
				var location = node.GetLocation();
				var lineNum = Helper.GetLineNumber(location);
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, lineNum));
			}
		}
	}
}
