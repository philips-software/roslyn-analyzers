// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LockObjectsMustBeReadonlyAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Objects used as locks should be readonly";
		private const string MessageFormat = @"'{0}' should be readonly";
		private const string Description = @"";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.LocksShouldBeReadonly), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.LockStatement);
		}

		private void Analyze(SyntaxNodeAnalysisContext obj)
		{
			LockStatementSyntax lockStatementSyntax = (LockStatementSyntax)obj.Node;

			if (lockStatementSyntax.Expression is IdentifierNameSyntax identifier)
			{
				SymbolInfo info = obj.SemanticModel.GetSymbolInfo(identifier);

				if (info.Symbol is IFieldSymbol field)
				{
					if (!field.IsReadOnly)
					{
						obj.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation(), identifier.ToString()));
					}
				}
			}


		}
	}
}
