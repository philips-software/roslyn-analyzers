// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidArrayList),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		private static readonly string ArrayListTypeName = "System.Collections.ArrayList";

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.VariableDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var variable = (VariableDeclarationSyntax)context.Node;
			var typeSymbol = context.SemanticModel.GetSymbolInfo(variable.Type).Symbol as INamedTypeSymbol;
			if (typeSymbol?.ToString() == ArrayListTypeName)
			{
				var variableName = variable.Variables.First().Identifier.Text;
				context.ReportDiagnostic(Diagnostic.Create(Rule, variable.Type.GetLocation(), variableName));
			}
		}
	}
}
