// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PositiveNamingAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Positive Naming";
		private const string MessageFormat = @"Properties and variables should be named using positive wording.";
		private const string Description = @"Properties and variables should be named using positive wording.";
		private const string Category = Categories.Naming;

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.PositiveNaming), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		private static readonly string[] negativeWords = { "disable", "ignore", "missing", "absent" };

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeVariable, SyntaxKind.VariableDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}

		private static void AnalyzeVariable(SyntaxNodeAnalysisContext context)
		{
			VariableDeclarationSyntax node = (VariableDeclarationSyntax)context.Node;

			if (Helper.IsInTestClass(context))
			{
				return;
			}

			var variables = node.Variables;
			if (!variables.Any())
			{
				return;
			}
			foreach (var variable in variables)
			{
				if (!IsPositiveName(variable.Identifier.Text))
				{
					CreateDiagnostic(context, node.GetLocation());
				}
			}
		}

		private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			PropertyDeclarationSyntax node = (PropertyDeclarationSyntax)context.Node;

			if (Helper.IsInTestClass(context))
			{
				return;
			}

			if (!IsPositiveName(node.Identifier.Text))
			{
				CreateDiagnostic(context, node.GetLocation());
			}
		}

		private static void CreateDiagnostic(SyntaxNodeAnalysisContext context, Location location)
		{
			Diagnostic diagnostic = Diagnostic.Create(Rule, location);
			context.ReportDiagnostic(diagnostic);
		}

		private static bool IsPositiveName(string name) {
			var lower = name.ToLowerInvariant();
			foreach (var word in negativeWords)
			{
				if (lower.Contains(word))
				{
					return false;
				}
			}
			return true;
		}
	}
}
