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
				if (!HasRequiredAttributes(presentAttributes))
				{
					return;
				}

				// Check if DataRow comes after TestMethod using the generic AttributeHelper method
				INamedTypeSymbol[] dataRowAttributes = { _definitions.DataRowSymbol };
				INamedTypeSymbol[] testMethodAttributes = { _definitions.TestMethodSymbol, _definitions.DataTestMethodSymbol };

				if (Helper.AttributeHelper.HasAttributeAfterOther(methodDeclaration.AttributeLists, context, dataRowAttributes, testMethodAttributes))
				{
					Location location = methodDeclaration.Identifier.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, methodDeclaration.Identifier));
				}
			}

			private bool HasRequiredAttributes(HashSet<INamedTypeSymbol> presentAttributes)
			{
				return presentAttributes.Contains(_definitions.DataRowSymbol) &&
					presentAttributes.Any(attr =>
						attr.IsDerivedFrom(_definitions.TestMethodSymbol) ||
						attr.IsDerivedFrom(_definitions.DataTestMethodSymbol));
			}
		}
	}
}
