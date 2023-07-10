﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidUsingParametersAsTempVariablesAnalyzer : DiagnosticAnalyzerBase
	{
		private const string TempTitle = @"Don't use parameters as temporary variables";
		private const string TempMessageFormat = @"Don't use parameter {0} as temporary variable, define a local variable instead.";
		private const string TempDescription = @"Don't use parameters as temporary variables, define a local variable instead.";
		private const string LoopTitle = @"Don't change loop variables";
		private const string LoopMessageFormat = @"Don't change loop variable {0}.";
		private const string LoopDescription = @"Don't change loop variables, this gives unexpected loop iterations. Use continue and break instead.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor TempRule = new(DiagnosticId.AvoidUsingParametersAsTempVariables.ToId(),
			TempTitle, TempMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false,
			description: TempDescription);
		private static readonly DiagnosticDescriptor LoopRule = new(DiagnosticId.AvoidChangingLoopVariables.ToId(),
			LoopTitle, LoopMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false,
			description: LoopDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(TempRule, LoopRule);

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleAssignmentExpression);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var assignment = (AssignmentExpressionSyntax)context.Node;
			if (assignment.Left is not IdentifierNameSyntax assigned)
			{
				return;
			}

			var assignedVariableName = assigned.Identifier.Text;
			// Check: Avoid using parameters as temporary variables.
			ParameterListSyntax parameters = assignment.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault()?.ParameterList;
			if (parameters != null && parameters.Parameters.Any(para => !para.Modifiers.Any(SyntaxKind.OutKeyword) && !para.Modifiers.Any(SyntaxKind.RefKeyword) && para.Identifier.Text == assignedVariableName))
			{
				Location location = assigned.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(TempRule, location, assignedVariableName));
			}

			VariableDeclaratorSyntax loopVariable = assignment.Ancestors().OfType<ForStatementSyntax>().FirstOrDefault()?.Declaration?.Variables.FirstOrDefault();
			// Check: Avoid changing loop variables.
			if (loopVariable != null && loopVariable.Identifier.Text == assignedVariableName)
			{
				Location location = assigned.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(LoopRule, location, assignedVariableName));
			}
		}
	}
}
