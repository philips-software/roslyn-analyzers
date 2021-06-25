// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;


namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]

	public class AvoidEmptyStatementBlocksAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"No empty statement blocks";
		public const string MessageFormat = @"Avoid statement blocks that are empty";
		private const string Description = @"Remove empty statement blocks";
		private const string Category = Categories.Maintainability;


		public DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidEmptyStatementBlock), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Block);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			BlockSyntax blockSyntax = (BlockSyntax)context.Node;
			if (!blockSyntax.Statements.Any())
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, blockSyntax.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
			else return;

		}
	}
}