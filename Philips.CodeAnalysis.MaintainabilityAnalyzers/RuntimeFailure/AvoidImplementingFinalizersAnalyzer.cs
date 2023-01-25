// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidImplementingFinalizersAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Don't implement a finalizer";
		private const string MessageFormat = @"Don't implement a finalizer, use Dispose instead.";
		private const string Description = @"Don't implement a finalizer, use Dispose instead.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidImplementingFinalizers),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.DestructorDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var finalizer = (DestructorDeclarationSyntax)context.Node;
			var loc = finalizer.GetLocation();
			context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
		}
	}
}
