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
	public class DataRowOrderAnalyzer : TestAttributeDiagnosticAnalyzer
	{
		private const string Title = @"Order DataRow attribute above TestMethod for unit tests";
		public static readonly string MessageFormat = @"DataRow attributes should be placed above TestMethod/DataTestMethod attributes";
		private const string Description = @"DataRow attributes should be consistently ordered above TestMethod/DataTestMethod attributes on test methods";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.DataRowOrderInTestMethod.ToId(),
												Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: false, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override Implementation OnInitializeAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return new DataRowOrderImplementation(definitions, Helper);
		}

		private sealed class DataRowOrderImplementation : Implementation
		{
			private readonly MsTestAttributeDefinitions _definitions;

			public DataRowOrderImplementation(MsTestAttributeDefinitions definitions, Helper helper) : base(helper)
			{
				_definitions = definitions;
			}

			public override void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, HashSet<INamedTypeSymbol> presentAttributes)
			{
				// Check if method has both DataRow and TestMethod/DataTestMethod attributes
				var hasDataRow = presentAttributes.Contains(_definitions.DataRowSymbol);
				var hasTestMethod = presentAttributes.Any(attr =>
					attr.IsDerivedFrom(_definitions.TestMethodSymbol) ||
					attr.IsDerivedFrom(_definitions.DataTestMethodSymbol));

				if (!hasDataRow || !hasTestMethod)
				{
					return;
				}

				// Check the order of attributes in the syntax
				SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;
				var testMethodPosition = -1;
				var hasDataRowAfterTestMethod = false;

				for (var listIndex = 0; listIndex < attributeLists.Count; listIndex++)
				{
					AttributeListSyntax attributeList = attributeLists[listIndex];
					for (var attrIndex = 0; attrIndex < attributeList.Attributes.Count; attrIndex++)
					{
						AttributeSyntax attribute = attributeList.Attributes[attrIndex];
						INamedTypeSymbol attributeSymbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol?.ContainingType;

						if (attributeSymbol != null)
						{
							var currentPosition = (listIndex * 1000) + attrIndex;

							if (attributeSymbol.IsDerivedFrom(_definitions.TestMethodSymbol) ||
								attributeSymbol.IsDerivedFrom(_definitions.DataTestMethodSymbol))
							{
								if (testMethodPosition == -1)
								{
									testMethodPosition = currentPosition;
								}
							}
							else if (SymbolEqualityComparer.Default.Equals(attributeSymbol, _definitions.DataRowSymbol))
							{
								// If we've seen a TestMethod and this DataRow comes after it
								if (testMethodPosition != -1 && currentPosition > testMethodPosition)
								{
									hasDataRowAfterTestMethod = true;
									break;
								}
							}
						}
					}

					if (hasDataRowAfterTestMethod)
					{
						break;
					}
				}

				// If DataRow comes after TestMethod, report diagnostic
				if (hasDataRowAfterTestMethod)
				{
					Location location = methodDeclaration.Identifier.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, methodDeclaration.Identifier));
				}
			}
		}
	}
}
