// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreventUseOfGotoAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Do not use goto";
		private const string MessageFormat = @"Do not use goto";
		private const string Description = @"Do not use goto";

		public PreventUseOfGotoAnalyzer()
			: base(DiagnosticId.GotoNotAllowed, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		private void Analyze(SyntaxNodeAnalysisContext analysisContext)
		{
			analysisContext.ReportDiagnostic(Diagnostic.Create(Rule, analysisContext.Node.GetLocation()));
		}

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.GotoStatement, SyntaxKind.LabeledStatement, SyntaxKind.GotoCaseStatement, SyntaxKind.GotoDefaultStatement);
		}
	}
}
