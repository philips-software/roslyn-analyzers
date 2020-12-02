// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidRedundantSwitchStatementAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Do not use redundant switch statements";
		private const string MessageFormat = @"Switch statement only has a default case.  Remove the switch statement and just use the default case code.";
		private const string Description = @"Elide the switch statement";
		private const string Category = Categories.Readability;

		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidSwitchStatementsWithNoCases), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterOperationAction(Analyze, OperationKind.Switch);
			context.RegisterOperationAction(AnalyzeExpression, OperationKind.SwitchExpression);
		}

		private void Analyze(OperationAnalysisContext obj)
		{
			ISwitchOperation operation = (ISwitchOperation)obj.Operation;

			if (operation.Cases.Length != 1)
			{
				return;
			}

			ISwitchCaseOperation caseOperation = operation.Cases[0];

			if (caseOperation.Clauses.Any(x => x.Label?.Name == "default"))
			{
				obj.ReportDiagnostic(Diagnostic.Create(Rule, operation.Cases[0].Syntax.GetLocation()));
				return;
			}
		}
		private void AnalyzeExpression(OperationAnalysisContext obj)
		{
			ISwitchExpressionOperation operation = (ISwitchExpressionOperation)obj.Operation;

			if (operation.Arms.Length != 1)
			{
				return;
			}

			ISwitchExpressionArmOperation caseOperation = operation.Arms[0];

			if (caseOperation.Pattern.Kind == OperationKind.DiscardPattern)
			{
				obj.ReportDiagnostic(Diagnostic.Create(Rule, operation.Arms[0].Syntax.GetLocation()));
				return;
			}
		}
	}
}
