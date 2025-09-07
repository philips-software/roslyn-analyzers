// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreferCombinatorialTestingAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"Use combinatorial testing";
		public const string MessageFormat = @"Use CombinatorialValues for this {0}-parameter method with {1} DataRow attributes covering {2}.";
		private const string Description = @"Methods with parameters and DataRow attributes covering all combinations can be simplified using CombinatorialValues from the Combinatorial.MSTest package.";
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
				var parameterCount = methodDeclaration.ParameterList.Parameters.Count;

				// Only analyze methods with 1 or 2 parameters
				if (parameterCount is not 1 and not 2)
				{
					return;
				}

				// Extract DataRow attributes and their arguments
				SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;
				var dataRowArguments = new List<List<object>>();

				foreach (AttributeListSyntax attributeList in attributeLists)
				{
					foreach (AttributeSyntax attribute in attributeList.Attributes)
					{
						if (Helper.ForAttributes.IsDataRowAttribute(attribute, context))
						{
							List<object> args = ExtractDataRowArguments(attribute, context);
							if (args != null)
							{
								dataRowArguments.Add(args);
							}
						}
					}
				}

				if (dataRowArguments.Count < MinimumDataRowsForSuggestion)
				{
					return;
				}

				// Check if this is a candidate for combinatorial testing
				if (ShouldSuggestCombinatorialTesting(dataRowArguments, parameterCount))
				{
					Location location = methodDeclaration.Identifier.GetLocation();
					var coverage = parameterCount == 1 ?
						"all parameter values" :
						"all parameter combinations";
					var diagnostic = Diagnostic.Create(Rule, location, parameterCount, dataRowArguments.Count, coverage);
					context.ReportDiagnostic(diagnostic);
				}
			}

			private List<object> ExtractDataRowArguments(AttributeSyntax attribute, SyntaxNodeAnalysisContext context)
			{
				if (attribute.ArgumentList?.Arguments == null)
				{
					return null;
				}

				var arguments = new List<object>();
				foreach (AttributeArgumentSyntax arg in attribute.ArgumentList.Arguments)
				{
					if (Helper.ForAttributes.TryExtractAttributeArgument<object>(arg, context, out _, out var value))
					{
						arguments.Add(value);
					}
					else
					{
						// If we can't extract a value, use the string representation
						arguments.Add(arg.Expression.ToString());
					}
				}

				return arguments.Count > 0 ? arguments : null;
			}

			private bool ShouldSuggestCombinatorialTesting(List<List<object>> dataRowArguments, int parameterCount)
			{
				if (parameterCount == 1)
				{
					// For single parameter: all DataRows must have exactly 1 argument
					return dataRowArguments.All(args => args.Count == 1);
				}
				else if (parameterCount == 2)
				{
					// For two parameters: check if we have complete combinatorial coverage
					if (!dataRowArguments.All(args => args.Count == 2))
					{
						return false;
					}

					// Get unique values for each parameter position
					var uniqueValuesParam1 = dataRowArguments.Select(args => args[0]).Distinct().ToList();
					var uniqueValuesParam2 = dataRowArguments.Select(args => args[1]).Distinct().ToList();

					// Check if we have all combinations
					var expectedCombinations = uniqueValuesParam1.Count * uniqueValuesParam2.Count;

					if (dataRowArguments.Count != expectedCombinations)
					{
						return false;
					}

					// Verify that all combinations are actually present
					var actualCombinations = new HashSet<(object, object)>(
						dataRowArguments.Select(args => (args[0], args[1])));
					var expectedCombinationSet = new HashSet<(object, object)>();

					foreach (var val1 in uniqueValuesParam1)
					{
						foreach (var val2 in uniqueValuesParam2)
						{
							_ = expectedCombinationSet.Add((val1, val2));
						}
					}

					return actualCombinations.SetEquals(expectedCombinationSet);
				}

				return false;
			}
		}
	}
}