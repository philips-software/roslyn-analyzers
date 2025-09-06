// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreferCombinatorialTestingAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"Consider using combinatorial testing";
		public const string MessageFormat = @"Consider using CombinatorialValues instead of {0} DataRow attributes for this single-parameter method.";
		private const string Description = @"Methods with a single parameter and multiple DataRow attributes can be simplified using CombinatorialValues from the Combinatorial.MSTest package.";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.PreferCombinatorialTestingOverDataRows.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			Helper helper = new(options, compilation);
			return new PreferCombinatorialTestingImplementation(helper, definitions);
		}

		public class PreferCombinatorialTestingImplementation : TestMethodImplementation
		{
			private const int MinimumDataRowsForSuggestion = 4; // Suggest when 4+ DataRows

			public PreferCombinatorialTestingImplementation(Helper helper, MsTestAttributeDefinitions definitions) : base(definitions, helper)
			{
			}

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				// Only analyze methods with exactly 1 parameter
				if (methodDeclaration.ParameterList.Parameters.Count != 1)
				{
					return;
				}

				// Count DataRow attributes and verify they all have exactly 1 argument
				SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;
				var dataRowCount = 0;
				var allDataRowsHaveSingleArgument = true;

				foreach (AttributeListSyntax attributeList in attributeLists)
				{
					foreach (AttributeSyntax attribute in attributeList.Attributes)
					{
						if (Helper.ForAttributes.IsDataRowAttribute(attribute, context))
						{
							dataRowCount++;

							// Check if this DataRow has exactly 1 argument
							if (attribute.ArgumentList?.Arguments.Count != 1)
							{
								allDataRowsHaveSingleArgument = false;
							}
						}
					}
				}

				// Only suggest if there are enough DataRows with single arguments
				if (dataRowCount >= MinimumDataRowsForSuggestion && allDataRowsHaveSingleArgument)
				{
					Location location = methodDeclaration.Identifier.GetLocation();
					var diagnostic = Diagnostic.Create(Rule, location, dataRowCount);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}