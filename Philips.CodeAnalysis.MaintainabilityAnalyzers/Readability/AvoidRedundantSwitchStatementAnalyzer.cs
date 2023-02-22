// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidRedundantSwitchStatementAnalyzer : SingleDiagnosticAnalyzer
	{
		private readonly GeneratedCodeAnalysisFlags _generatedCodeFlags;

		private const string Title = @"Do not use redundant switch statements";
		private const string MessageFormat = @"Switch statement only has a default case.  Remove the switch statement and just use the default case code.";
		private const string Description = @"Elide the switch statement";

		public AvoidRedundantSwitchStatementAnalyzer()
			: this(GeneratedCodeAnalysisFlags.None)
		{ }

		public AvoidRedundantSwitchStatementAnalyzer(GeneratedCodeAnalysisFlags generatedCodeFlags)
			: base(DiagnosticId.AvoidSwitchStatementsWithNoCases, Title, MessageFormat, Description, Categories.Readability)
		{
			_generatedCodeFlags = generatedCodeFlags;
		}

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(_generatedCodeFlags);
			context.EnableConcurrentExecution();
			context.RegisterOperationAction(Analyze, OperationKind.Switch);
			context.RegisterOperationAction(AnalyzeExpression, OperationKind.SwitchExpression);
		}

		private void Analyze(OperationAnalysisContext operationContext)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(operationContext))
			{
				return;
			}

			var operation = (ISwitchOperation)operationContext.Operation;
			if (operation.Cases.Length != 1)
			{
				return;
			}

			ISwitchCaseOperation caseOperation = operation.Cases[0];

			if (!caseOperation.Clauses.Any(x => x.Label?.Name == "default"))
			{
				return;
			}

			Location location = operation.Cases[0].Syntax.GetLocation();
			operationContext.ReportDiagnostic(Diagnostic.Create(Rule, location));
		}
		private void AnalyzeExpression(OperationAnalysisContext operationContext)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(operationContext))
			{
				return;
			}

			var operation = (ISwitchExpressionOperation)operationContext.Operation;
			if (operation.Arms.Length != 1)
			{
				return;
			}

			ISwitchExpressionArmOperation caseOperation = operation.Arms[0];

			if (caseOperation.Pattern.Kind != OperationKind.DiscardPattern)
			{
				return;
			}

			Location location = operation.Arms[0].Syntax.GetLocation();
			operationContext.ReportDiagnostic(Diagnostic.Create(Rule, location));
		}
	}
}
