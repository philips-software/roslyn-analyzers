// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestMethodsMustHaveTheCorrectNumberOfArgumentsAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"TestMethods/DataTestMethods must have the correct number of arguments";
		public static readonly string MessageFormat = @"'{0}' has the wrong number of parameters";
		private const string Description = @"DataTestMethods should have the same number of parameters of the DataRows, TestMethods should have no arguments";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.TestMethodsMustHaveTheCorrectNumberOfArguments.ToId(),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return new TestMethodsMustHaveTheCorrectNumberOfArguments(definitions, Helper);
		}

		private sealed class TestMethodsMustHaveTheCorrectNumberOfArguments : TestMethodImplementation
		{
			public TestMethodsMustHaveTheCorrectNumberOfArguments(MsTestAttributeDefinitions definitions, Helper helper) : base(definitions, helper)
			{ }

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				int? expectedNumberOfParameters;
				if (isDataTestMethod)
				{
					if (HasParams(methodDeclaration))
					{
						return;
					}

					if (!TryGetExpectedParameters(methodDeclaration, context, out expectedNumberOfParameters))
					{
						Location location = methodDeclaration.Identifier.GetLocation();
						context.ReportDiagnostic(Diagnostic.Create(Rule, location, methodDeclaration.Identifier));
						return;
					}
				}
				else
				{
					expectedNumberOfParameters = 0;
				}

				if (expectedNumberOfParameters != null && expectedNumberOfParameters != methodDeclaration.ParameterList.Parameters.Count)
				{
					Location location = methodDeclaration.Identifier.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, methodDeclaration.Identifier));
				}
			}

			private bool HasParams(MethodDeclarationSyntax methodDeclaration)
			{
				return methodDeclaration.ParameterList.Parameters.LastOrDefault()?.Modifiers.Any(SyntaxKind.ParamsKeyword) == true;
			}

			private void CollectSupportingData(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration,
												out bool hasAnyCustomDataSources, out bool hasAnyDynamicData, out HashSet<int> dataRowParameters)
			{
				hasAnyCustomDataSources = false;
				hasAnyDynamicData = false;
				dataRowParameters = [];
				foreach (AttributeSyntax attribute in methodDeclaration.AttributeLists.SelectMany(x => x.Attributes))
				{
					if (Helper.ForAttributes.IsDataRowAttribute(attribute, context))
					{
						var argumentCount = attribute.ArgumentList.Arguments.Count((arg) => { return arg.NameEquals?.Name.Identifier.ValueText != @"DisplayName"; });
						_ = dataRowParameters.Add(argumentCount);
						continue;
					}

					if (Helper.ForAttributes.IsAttribute(attribute, context, MsTestFrameworkDefinitions.DynamicDataAttribute, out _, out _))
					{
						hasAnyDynamicData = true;
						continue;
					}

					SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(attribute);
					if (symbol.Symbol is IMethodSymbol method)
					{
						if (method.ContainingType.AllInterfaces.Contains(Definitions.ITestSourceSymbol))
						{
							hasAnyCustomDataSources = true;
						}

						continue;
					}
				}
			}

			private bool TryGetExpectedParameters(MethodDeclarationSyntax methodDeclaration, SyntaxNodeAnalysisContext context, out int? expectedNumberOfParameters)
			{
				CollectSupportingData(context, methodDeclaration, out var anyCustomDataSources, out var anyDynamicData, out HashSet<int> dataRowParameters);

				if (anyDynamicData || anyCustomDataSources)
				{
					expectedNumberOfParameters = null;

					return dataRowParameters.Count == 0;
				}

				if (dataRowParameters.Count == 0)
				{
					expectedNumberOfParameters = 0;
					return true;
				}

				if (dataRowParameters.Count != 1)
				{
					expectedNumberOfParameters = 0;
					return false;
				}

				expectedNumberOfParameters = dataRowParameters.First();
				return true;
			}
		}
	}
}
