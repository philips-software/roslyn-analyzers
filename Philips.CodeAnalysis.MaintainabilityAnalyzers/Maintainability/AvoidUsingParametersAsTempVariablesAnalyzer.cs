// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidUsingParametersAsTempVariablesAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Don't use parameters as temporary variables";
		private const string MessageFormat = @"Don't use parameter {0} as temporary variable, define a local variable instead.";
		private const string Description = @"Don't use parameters as temporary variables, define a local variable instead.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidUsingParametersAsTempVariables),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleAssignmentExpression);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector detector = new();
			if (detector.IsGeneratedCode(context))
			{
				return;
			}

			var assignment = (AssignmentExpressionSyntax)context.Node;
			if (assignment.Left is not IdentifierNameSyntax assigned)
			{
				return;
			}

			var parameters = assignment.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault()?.ParameterList;
			if (parameters == null)
			{
				return;
			}

			if (parameters.Parameters.Any(para => para.Identifier.Text == assigned.Identifier.Text))
			{
				var parameterName = assigned.Identifier.Text;
				context.ReportDiagnostic(Diagnostic.Create(Rule, assigned.GetLocation(), parameterName));
			}

			var loopVariable = assignment.Ancestors().OfType<ForStatementSyntax>().FirstOrDefault()?.Declaration?.Variables.First();
			if (loopVariable == null)
			{
				return;
			}

			if (loopVariable.Identifier.Text == assigned.Identifier.Text)
			{
				var parameterName = assigned.Identifier.Text;
				context.ReportDiagnostic(Diagnostic.Create(Rule, assigned.GetLocation(), parameterName));
			}
		}
	}
}
