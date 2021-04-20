// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	/// <summary>
	/// Report when a multi line if statement does not include a block.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LimitConditionComplexityAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = "Limit the number of checks in a condition";
		private const string Message =
			"Divide the condition statement around line {0} such, that the number of check is not larger than {1}.";
		private const string Description = "Divide long conditions in consistent way";
		private const string Category = Categories.Readability;

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.LimitConditionComplexity),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: Description);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		/// <summary>
		/// <inheritdoc/>
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
			var filePath = context.Node.SyntaxTree.FilePath;
			if (Helper.IsGeneratedCode(filePath))
			{
				return;
			}

			var ifStatement = ((IfStatementSyntax)context.Node).Condition;
			AnalyzeCondition(context, ifStatement);
		}

		private void AnalyzeTernary(SyntaxNodeAnalysisContext context)
		{
			var filePath = context.Node.SyntaxTree.FilePath;
			if (Helper.IsGeneratedCode(filePath))
			{
				return;
			}
			var condition = ((ConditionalExpressionSyntax)context.Node).Condition;
			AnalyzeCondition(context, condition);
		}

		private void AnalyzeCondition(SyntaxNodeAnalysisContext context, ExpressionSyntax conditionNode)
		{
			var numOperators = conditionNode.DescendantTokens().Where(IsLogicalOperator).Count();
			var additionalFilesHelper = new AdditionalFilesHelper(context.Options, context.Compilation);
			var configValue = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"max_operators");
			if (int.TryParse(configValue, out int maxOperators))
			{
				if (numOperators >= maxOperators)
				{
					var newLineLocation = conditionNode.GetLocation();
					var lineNum = Helper.GetLineNumber(newLineLocation);
					context.ReportDiagnostic(Diagnostic.Create(Rule, newLineLocation, lineNum));
				}
			}
		}

		private bool IsLogicalOperator(SyntaxToken token)
		{
			return 
				token.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
				token.IsKind(SyntaxKind.BarBarToken);
		}
	}
}
