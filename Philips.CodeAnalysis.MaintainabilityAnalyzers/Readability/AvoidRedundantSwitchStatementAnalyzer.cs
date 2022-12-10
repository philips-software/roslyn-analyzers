// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidRedundantSwitchStatementAnalyzer : DiagnosticAnalyzer
	{
		private readonly GeneratedCodeDetector _generatedCodeDetector;

		public AvoidRedundantSwitchStatementAnalyzer(GeneratedCodeDetector generatedCodeDetector)
		{
			_generatedCodeDetector = generatedCodeDetector;
		}
		public AvoidRedundantSwitchStatementAnalyzer()
			: this(new GeneratedCodeDetector())
		{ }

		private const string Title = @"Do not use redundant switch statements";
		private const string MessageFormat = @"Switch statement only has a default case.  Remove the switch statement and just use the default case code.";
		private const string Description = @"Elide the switch statement";
		private const string Category = Categories.Readability;

		public static DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidSwitchStatementsWithNoCases), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterOperationAction(Analyze, OperationKind.Switch);
			context.RegisterOperationAction(AnalyzeExpression, OperationKind.SwitchExpression);
		}

		private void Analyze(OperationAnalysisContext operationContext)
		{
			ISwitchOperation operation = (ISwitchOperation)operationContext.Operation;
			if (operation.Cases.Length != 1)
			{
				return;
			}

			ISwitchCaseOperation caseOperation = operation.Cases[0];

			if (!caseOperation.Clauses.Any(x => x.Label?.Name == "default"))
			{
				return;
			}

			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(operationContext))
			{
				return;
			}

			operationContext.ReportDiagnostic(Diagnostic.Create(Rule, operation.Cases[0].Syntax.GetLocation()));
		}
		private void AnalyzeExpression(OperationAnalysisContext operationContext)
		{
			ISwitchExpressionOperation operation = (ISwitchExpressionOperation)operationContext.Operation;

			if (operation.Arms.Length != 1)
			{
				return;
			}

			ISwitchExpressionArmOperation caseOperation = operation.Arms[0];

			if (caseOperation.Pattern.Kind != OperationKind.DiscardPattern)
			{
				return;
			}

			if (_generatedCodeDetector.IsGeneratedCode(operationContext))
			{
				return;
			}

			operationContext.ReportDiagnostic(Diagnostic.Create(Rule, operation.Arms[0].Syntax.GetLocation()));
		}
	}
}
