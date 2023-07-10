// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Globalization;
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
	public class LimitConditionComplexityAnalyzer : DiagnosticAnalyzerBase
	{
		private const string Title = "Limit the number of clauses in a condition";
		private const string Message =
			"Found {0} clauses in the condition statement. Limit the number of clauses in the condition statement, to not more than {1} (or define max_operators in .editorconfig to customize).";
		private const string Description = Title;
		private const string Category = Categories.Readability;

		private static readonly DiagnosticDescriptor Rule =
			new(
				DiagnosticId.LimitConditionComplexity.ToId(),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: false,
				description: Description);

		private int _maxOperators;

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public static int DefaultMaxOperators { get; private set; } = 4;

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			var maxStr = Helper.ForAdditionalFiles.GetValueFromEditorConfig(Rule.Id, "max_operators");
			if (int.TryParse(maxStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMax))
			{
				_maxOperators = parsedMax;
			}
			else
			{
				_maxOperators = DefaultMaxOperators;
			}
			context.RegisterSyntaxNodeAction(
				AnalyzeIfStatement,
				SyntaxKind.IfStatement);
			context.RegisterSyntaxNodeAction(
				AnalyzeTernary,
				SyntaxKind.ConditionalExpression);
		}

		private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
		{
			ExpressionSyntax ifStatement = ((IfStatementSyntax)context.Node).Condition;
			AnalyzeCondition(context, ifStatement);
		}

		private void AnalyzeTernary(SyntaxNodeAnalysisContext context)
		{
			ExpressionSyntax condition = ((ConditionalExpressionSyntax)context.Node).Condition;
			AnalyzeCondition(context, condition);
		}

		private void AnalyzeCondition(SyntaxNodeAnalysisContext context, ExpressionSyntax conditionNode)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var numOperators = conditionNode.DescendantTokens().Count(IsLogicalOperator);
			if (numOperators >= _maxOperators)
			{
				Location newLineLocation = conditionNode.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, newLineLocation, numOperators, _maxOperators));
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
