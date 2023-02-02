// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidArrayListAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Don't use ArrayList, use List<T> instead";
		private const string MessageFormat = @"Don't use ArrayList for variable {0}, use List<T> instead";
		private const string Description = @"Usage of Arraylist is discouraged by Microsoft for performance reasons, use List<T> instead.";
		private const string Category = Categories.Maintainability;

		private const string ArrayListTypeName = "System.Collections.ArrayList";

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidArrayList),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.VariableDeclaration);
		}
		
		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var variable = (VariableDeclarationSyntax)context.Node;
			if (variable.Type is not SimpleNameSyntax typeName)
			{
				// Full (or partial) namespace syntax, check right-most entry only.
				if (variable.Type is QualifiedNameSyntax qualifiedName)
				{
					typeName = qualifiedName.Right;
				}
				else
				{
					// Some thing else is mentioned here.
					return;
				}
			}

			if (!typeName.Identifier.Text.Contains("ArrayList"))
			{
				return;
			}

			// Sanity check if we got ArrayList from the correct namespace.
			var typeSymbol = context.SemanticModel.GetSymbolInfo(variable.Type).Symbol as INamedTypeSymbol;
			if (typeSymbol?.ToString() == ArrayListTypeName)
			{
				var variableName = variable.Variables.FirstOrDefault()?.Identifier.Text ?? string.Empty;
				context.ReportDiagnostic(Diagnostic.Create(Rule, typeName.GetLocation(), variableName));
			}
		}
	}
}
