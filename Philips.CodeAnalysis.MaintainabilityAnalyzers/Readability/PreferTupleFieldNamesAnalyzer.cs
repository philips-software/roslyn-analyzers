// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreferTupleFieldNamesAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Prefer tuple field names over generic item1, item2, etc";
		private const string MessageFormat = @"Use the name '{0}' for this field instead of '{1}'";
		private const string Description = @"For readability use the name provided for this field, not a generic field name";
		private const string Category = Categories.Readability;

		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.PreferUsingNamedTupleField), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName(StringConstants.TupleFullyQualifiedName) == null)
				{
					return;
				}

				startContext.RegisterOperationAction(AnalyzeAccess, OperationKind.FieldReference);
			});
		}

		private static void AnalyzeAccess(OperationAnalysisContext context)
		{
			IFieldReferenceOperation fieldReference = (IFieldReferenceOperation)context.Operation;

			if (fieldReference.Instance == null)
			{
				//static method, don't care
				return;
			}

			IFieldSymbol field = fieldReference.Field;
			INamedTypeSymbol namedTypeSymbol = field.ContainingType;

			if (!namedTypeSymbol.IsTupleType)
			{
				return;
			}


			if (!SymbolEqualityComparer.Default.Equals(field.CorrespondingTupleField, field))
			{
				// they are not using the Item1, Item2, Item3, etc.
				return;
			}

			// they are using Item1/Item2/ etc.  Detect if that field has a name they can use instead.
			foreach (var element in namedTypeSymbol.TupleElements)
			{
				if (!SymbolEqualityComparer.Default.Equals(element.CorrespondingTupleField, field))
				{
					continue;
				}

				if (element.Name == element.CorrespondingTupleField.Name)
				{
					continue;
				}

				context.ReportDiagnostic(Diagnostic.Create(Rule, fieldReference.Syntax.GetLocation(), element.Name, field.Name));
			}
		}
	}
}
