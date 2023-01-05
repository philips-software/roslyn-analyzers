// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestHasCategoryAttributeAnalyzer : TestMethodDiagnosticAnalyzer
	{
		public const string FileName = @"TestsWithUnsupportedCategory.Allowed.txt";
		private const string Title = @"Test must have an appropriate TestCategory";
		public const string MessageFormat = @"Test must have an appropriate TestCategory attribute. Check EditorConfig";
		private const string Description = @"Tests are required to have an appropriate TestCategory to allow running tests category wise.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.TestHasCategoryAttribute), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			AdditionalFilesHelper helper = new(options, compilation);

			var exceptions = helper.LoadExceptions(FileName);
			var allowedCategories = helper.GetValuesFromEditorConfig(Rule.Id, @"allowed_test_categories").ToImmutableHashSet();

			return new TestHasCategoryAttribute(exceptions, allowedCategories, definitions);
		}

		public class TestHasCategoryAttribute : TestMethodImplementation
		{
			private readonly HashSet<string> _exceptions;
			private readonly ImmutableHashSet<string> _allowedCategories;

			public TestHasCategoryAttribute(HashSet<string> exceptions, ImmutableHashSet<string> allowedCategories, MsTestAttributeDefinitions definitions) : base(definitions)
			{
				_exceptions = exceptions;
				_allowedCategories = allowedCategories;
			}

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;

				if (methodDeclaration.Parent is ClassDeclarationSyntax classDeclaration)
				{
					if (_exceptions.Contains($"{classDeclaration.Identifier.Text}.{methodDeclaration.Identifier.Text}"))
					{
						return;
					}
				}

				if (!Helper.HasAttribute(attributeLists, context, MsTestFrameworkDefinitions.TestCategoryAttribute, out Location categoryLocation, out AttributeArgumentSyntax argumentSyntax))
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation());
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
		}
	}
}
