// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class TestHasCategoryAnalyzer : TestMethodDiagnosticAnalyzer
	{
		public const string FileName = @"TestsWithUnsupportedCategory.Allowed.txt";
		private const string Title = @"Test must have an appropriate TestCategory";
		public const string MessageFormat = @"Test must have an appropriate TestCategory attribute. Check EditorConfig";
		private const string Description = @"Tests are required to have an appropriate TestCategory to allow running tests category wise.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.TestHasCategoryAttribute), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			AdditionalFilesHelper helper = new(options, compilation);
			var allowedCategories = helper.GetValuesFromEditorConfig(Rule.Id, @"allowed_test_categories");
			AllowedSymbols allowedSymbols = new(compilation);
			allowedSymbols.Initialize(options.AdditionalFiles, FileName);

			return new TestHasAttributeCategory(allowedSymbols, allowedCategories, definitions);
		}

		public class TestHasAttributeCategory : TestMethodImplementation
		{
			private readonly AllowedSymbols _allowedSymbols;
			private readonly ImmutableHashSet<string> _allowedCategories;

			public TestHasAttributeCategory(AllowedSymbols allowedSymbols, IReadOnlyList<string> allowedCategories, MsTestAttributeDefinitions definitions) : base(definitions)
			{
				_allowedSymbols = allowedSymbols;
				_allowedCategories = allowedCategories.Select(cat => TrimStart(cat, "~T:")).ToImmutableHashSet();
			}

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;

				if (
					context.SemanticModel.GetDeclaredSymbol(methodDeclaration) is IMethodSymbol symbol &&
					_allowedSymbols.IsAllowed(symbol))
				{
					return;
				}

				if (!AttributeHelper.HasAttribute(attributeLists, context, MsTestFrameworkDefinitions.TestCategoryAttribute, out Location categoryLocation, out AttributeArgumentSyntax argumentSyntax))
				{
					var location = methodDeclaration.Identifier.GetLocation();
					Diagnostic diagnostic = Diagnostic.Create(Rule, location);
					context.ReportDiagnostic(diagnostic);
					return;
				}

				string value;
				switch (argumentSyntax.Expression)
				{
					case MemberAccessExpressionSyntax mae:
						value = mae.ToString();
						break;
					case LiteralExpressionSyntax literal:
						value = literal.ToString();
						break;
					default:
						return;
				}

				if (!_allowedCategories.Contains(value))
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, categoryLocation);
					context.ReportDiagnostic(diagnostic);
				}
			}

			private static string TrimStart(string victim, string piece)
			{
				var index = victim.IndexOf(piece);
				if(index > 0)
				{
					return victim.Substring(index);
				}

				return victim;
			}
		}
	}
}
