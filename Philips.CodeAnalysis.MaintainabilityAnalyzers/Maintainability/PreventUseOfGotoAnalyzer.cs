// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreventUseOfGotoAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Do not use goto";
		private const string MessageFormat = Title;
		private const string Description = Title;

		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.GotoNotAllowed), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		private void Analyze(SyntaxNodeAnalysisContext analysisContext)
		{
			analysisContext.ReportDiagnostic(Diagnostic.Create(Rule, analysisContext.Node.GetLocation()));
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.GotoStatement, SyntaxKind.LabeledStatement, SyntaxKind.GotoCaseStatement, SyntaxKind.GotoDefaultStatement);
		}
	}
}
