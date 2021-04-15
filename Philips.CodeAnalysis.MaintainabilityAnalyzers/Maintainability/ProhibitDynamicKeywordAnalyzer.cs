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
	public class ProhibitDynamicKeywordAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Prohibit the ""dynamic"" Keyword";
		private const string MessageFormat = @"Do not use the ""dynamic"" keyword.  It it not compile time type safe.";
		private const string Description = @"The ""dynamic"" keyword is not checked for type safety at compile time and is prohibited.";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.DynamicKeywordProhibited), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IdentifierName);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			IdentifierNameSyntax identifierNameSyntax = (IdentifierNameSyntax)context.Node;

			if (!IsIdentifierDynamicType(context, identifierNameSyntax))
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, identifierNameSyntax.GetLocation()));
		}

		private bool IsIdentifierDynamicType(SyntaxNodeAnalysisContext context, IdentifierNameSyntax identifierNameSyntax)
		{
			if (identifierNameSyntax.Identifier.ValueText == "dynamic" && !identifierNameSyntax.Parent.IsKind(SyntaxKind.Argument) &&
				!identifierNameSyntax.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
			{
				return true;
			}

			if (identifierNameSyntax.IsVar)
			{
				SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(identifierNameSyntax);

				if (symbol.Symbol is IDynamicTypeSymbol)
				{
					return true;
				}
			}

			return false;
		}
	}
}
