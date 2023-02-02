// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidMsFakesAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid MS Fakes";
		public const string MessageFormat = @"Do not use MS Fakes as a Dependency Injection solution.";
		private const string Description = @"Avoid MS Fakes. Use Moq instead for example.  If applicable, remove the Reference and the .fakes file as well.";
		private const string Category = Categories.Maintainability;

		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidMsFakes), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.UsingStatement);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is not UsingStatementSyntax usingStatement)
			{
				return;
			}

			ExpressionSyntax expression = usingStatement.Expression;
			if (expression == null)
			{
				return;
			}

			if (expression.ToString().Contains(@"ShimsContext.Create"))
			{
				CSharpSyntaxNode violation = expression;
				var location = violation.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(Rule, location);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
