// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreferNamedTuplesAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Prefer tuples that have names";
		private const string MessageFormat = @"Name this tuple field";
		private const string Description = @"Name this tuple field for readability";
		private const string Category = Categories.Readability;

		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.PreferTuplesWithNamedFields), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("System.ValueTuple") == null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction(AnalyzeTupleType, SyntaxKind.TupleType);
			});
		}

		private static void AnalyzeTupleType(SyntaxNodeAnalysisContext context)
		{
			TupleTypeSyntax tupleExpressionSyntax = (TupleTypeSyntax)context.Node;

			foreach (var element in tupleExpressionSyntax.Elements)
			{
				if (element.Identifier.Kind() == SyntaxKind.None)
				{
					context.ReportDiagnostic(Diagnostic.Create(Rule, element.GetLocation()));
				}
			}
		}
	}
}
