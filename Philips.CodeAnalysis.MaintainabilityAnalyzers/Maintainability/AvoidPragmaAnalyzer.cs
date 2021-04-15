// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidPragmaAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid Pragma Warning";
		public const string MessageFormat = @"Do not use #pragma warning";
		private const string Description = @"Do not use #pragma warning";
		private const string Category = Categories.Maintainability;

		public List<DiagnosticDescriptor> Rules = new List<DiagnosticDescriptor>()
		{
			new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidPragma), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description),
		};

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rules.ToArray()); } }

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.PragmaWarningDirectiveTrivia);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			PragmaWarningDirectiveTriviaSyntax pragma = context.Node as PragmaWarningDirectiveTriviaSyntax;
			if (pragma == null)
			{
				return;
			}

			CSharpSyntaxNode violation = pragma;
			Diagnostic diagnostic = Diagnostic.Create(Rules[0], violation.GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}
