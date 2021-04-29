// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	/// <summary>
	/// Report when a multi line if statement does not include a block.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LimitConditionComplexityAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = "Limit the number of clauses in a condition";
		private const string Message =
			"Limit the number of clauses in the condition statement, to not more than {0}.";
		private const string Description = "Limit the number of clauses in a condition";
		private const string Category = Categories.Readability;

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.LimitConditionComplexity),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: false,
				description: Description);

		private int maxOperators;

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
			context.RegisterCompilationStartAction(
				startContext =>
				{
					var additionalFiles = new AdditionalFilesHelper(
						startContext.Options,
						startContext.Compilation);
					var maxStr = additionalFiles.GetValueFromEditorConfig(Rule.Id, "max_operators");
					if (int.TryParse(maxStr, out int parsedMax))
					{
						maxOperators = parsedMax;
						startContext.RegisterSyntaxNodeAction(
							AnalyzeIfStatement,
							SyntaxKind.IfStatement);
						startContext.RegisterSyntaxNodeAction(
							AnalyzeTernary,
							SyntaxKind.ConditionalExpression);
					}
					else
					{
						startContext.RegisterCompilationEndAction(ReportParsingError);
					}
				});
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
			if (numOperators >= maxOperators)
			{
				var newLineLocation = conditionNode.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, newLineLocation, numOperators));
			}
		}

		private void ReportParsingError(CompilationAnalysisContext context)
		{
			var loc = Location.Create(context.Compilation.SyntaxTrees.First(), TextSpan.FromBounds(0, 0));
			context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
		}

		private bool IsLogicalOperator(SyntaxToken token)
		{
			return 
				token.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
				token.IsKind(SyntaxKind.BarBarToken);
		}
	}
}
