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
		public const string MessageFormat = @"Consider using combinatorial testing instead of multiple DataRow attributes. This method has {0} DataRow attributes.";
		private const string Description = @"Methods with multiple DataRow attributes may benefit from combinatorial testing using the Combinatorial.MSTest package for better maintainability.";
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
				// Count DataRow attributes
				SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;
				var dataRowCount = 0;

				foreach (AttributeListSyntax attributeList in attributeLists)
				{
					foreach (AttributeSyntax attribute in attributeList.Attributes)
					{
						if (Helper.ForAttributes.IsDataRowAttribute(attribute, context))
						{
							dataRowCount++;
						}
					}
				}

				// Only suggest if there are enough DataRows
				if (dataRowCount >= MinimumDataRowsForSuggestion)
				{
					Location location = methodDeclaration.Identifier.GetLocation();
					var diagnostic = Diagnostic.Create(Rule, location, dataRowCount);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}