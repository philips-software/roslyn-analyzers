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
		#region Non-Public Data Members

		private const string Title = @"Do not use goto";
		private const string MessageFormat = @"Do not use goto";
		private const string Description = @"Do not use goto";

		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.GotoNotAllowed), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		#endregion

		#region Non-Public Properties/Methods

		private void Analyze(SyntaxNodeAnalysisContext analysisContext)
		{
			analysisContext.ReportDiagnostic(Diagnostic.Create(Rule, analysisContext.Node.GetLocation()));
		}

		#endregion

		#region Public Interface

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.GotoStatement, SyntaxKind.LabeledStatement, SyntaxKind.GotoCaseStatement, SyntaxKind.GotoDefaultStatement);
		}

		#endregion
	}
}
