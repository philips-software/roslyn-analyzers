// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class TestClassPublicMethodShouldBeTestMethodAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Test class public method should be a Test method";
		public const string MessageFormat = @"Public method should either be a test method or non-public";
		private const string Description = @"Test class cannot have a public method unless its a test method. Either change the access modifier or make it a test method";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.TestClassPublicMethodShouldBeTestMethod.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		private readonly TestHelper _testHelper;
		public TestClassPublicMethodShouldBeTestMethodAnalyzer()
			: this(new TestHelper())
		{ }
		public TestClassPublicMethodShouldBeTestMethodAnalyzer(TestHelper testHelper)
		{
			_testHelper = testHelper;
		}

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.AssemblyCleanupAttribute.FullName) == null)
				{
					return;
				}

				var definitions = MsTestAttributeDefinitions.FromCompilation(startContext.Compilation);

				startContext.RegisterSyntaxNodeAction((x) => Analyze(definitions, x), SyntaxKind.MethodDeclaration);
			});
		}

		private void Analyze(MsTestAttributeDefinitions definitions, SyntaxNodeAnalysisContext context)
		{
			var methodDeclaration = (MethodDeclarationSyntax)context.Node;

			if (!methodDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
			{
				return;
			}

			if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
			{
				return;
			}

			if (!_testHelper.IsTestClass(classDeclaration, context))
			{
				return;
			}

			ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

			if (symbol is null)
			{
				return;
			}

			var isAllowedToBePublic = false;
			foreach (INamedTypeSymbol attribute in symbol.GetAttributes().Select(attr => attr.AttributeClass))
			{
				if (definitions.NonTestMethods.Contains(attribute))
				{
					isAllowedToBePublic = true;
					break;
				}

				if (attribute.IsDerivedFrom(definitions.TestMethodSymbol))
				{
					isAllowedToBePublic = true;
					break;
				}
			}

			if (!isAllowedToBePublic)
			{
				Location location = methodDeclaration.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
