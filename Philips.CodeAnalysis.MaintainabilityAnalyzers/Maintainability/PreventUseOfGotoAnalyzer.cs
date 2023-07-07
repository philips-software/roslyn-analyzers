// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
		private const string MessageFormat = Title;
		private const string Description = Title;

		public PreventUseOfGotoAnalyzer()
			: base(DiagnosticId.GotoNotAllowed, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		private void Analyze(SyntaxNodeAnalysisContext analysisContext)
		{
			Location location = analysisContext.Node.GetLocation();
			analysisContext.ReportDiagnostic(Diagnostic.Create(Rule, location));
		}

		protected override void InitializeAnalysis(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.GotoStatement, SyntaxKind.LabeledStatement, SyntaxKind.GotoCaseStatement, SyntaxKind.GotoDefaultStatement);
		}
	}
}
