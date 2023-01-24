// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CastCompleteObjectAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Cast the complete object";
		private const string MessageFormat = @"This casts down to one of the field types, but not all of them. Consider to move this into an AsType() or ToType() method instead.";
		private const string Description = @"A cast should include all information from the previous type. By casting to a type of one of the fields, the cast is losing information. Use an AsType() or ToType() method instead.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.CastCompleteObject),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ConversionOperatorDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var conversion = (ConversionOperatorDeclarationSyntax)context.Node;
			var container = conversion.ParameterList.Parameters.FirstOrDefault()?.Type;
			// TODO: Consider banning explicitly casting to string, in favor of overriding ToString().
			if (
				container == null ||
				context.SemanticModel.GetSymbolInfo(conversion.Type).Symbol is not INamedTypeSymbol convertTo ||
				context.SemanticModel.GetSymbolInfo(container).Symbol is not INamedTypeSymbol containingType)
			{
				return;
			}
			var itsFields = containingType.GetMembers().OfType<IFieldSymbol>();

			if (itsFields is not null && itsFields.Count() > 1 && itsFields.Any(f => f.Type.Name == convertTo.Name))
			{
				var loc = conversion.Type.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
			}
		}
	}
}
