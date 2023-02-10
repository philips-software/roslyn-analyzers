﻿// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
		private readonly GeneratedCodeDetector _detector = new();

		public AvoidAssignmentInConditionAnalyzer()
			: base(DiagnosticId.AvoidAssignmentInCondition, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
			context.RegisterSyntaxNodeAction(AnalyzeTernary, SyntaxKind.ConditionalExpression);
		}

		private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
		{
			if (_detector.IsGeneratedCode(context))
			{
				return;
			}

			var ifStatement = ((IfStatementSyntax)context.Node).Condition;
			CheckDescendantHasNoAssignment(context, ifStatement);
		}

		private void AnalyzeTernary(SyntaxNodeAnalysisContext context)
		{
			if (_detector.IsGeneratedCode(context))
			{
				return;
			}

			var condition = ((ConditionalExpressionSyntax)context.Node).Condition;
			CheckDescendantHasNoAssignment(context, condition);
		}

		private void CheckDescendantHasNoAssignment(SyntaxNodeAnalysisContext context, SyntaxNode node)
		{
			if (node.DescendantTokens().Any(child => child.IsKind(SyntaxKind.EqualsToken)))
			{
				var location = node.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location));
			}
		}
	}
}
