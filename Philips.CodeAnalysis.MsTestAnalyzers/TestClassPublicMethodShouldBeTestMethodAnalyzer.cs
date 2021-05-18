// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
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
		private const string Category = Categories.Maintainability;

		private static AttributeDefinition[] attributeDefinitions =
		{
			MsTestFrameworkDefinitions.TestMethodAttribute,
			MsTestFrameworkDefinitions.DataTestMethodAttribute,
			MsTestFrameworkDefinitions.AssemblyInitializeAttribute,
			MsTestFrameworkDefinitions.AssemblyCleanupAttribute,
			MsTestFrameworkDefinitions.TestInitializeAttribute,
			MsTestFrameworkDefinitions.TestCleanupAttribute,
			MsTestFrameworkDefinitions.ClassCleanupAttribute,
			MsTestFrameworkDefinitions.ClassInitializeAttribute
		};

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.TestClassPublicMethodShouldBeTestMethod), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.AssemblyCleanupAttribute.FullName) == null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
			});
		}

		public static void Analyze(SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)context.Node;

			if (!methodDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
			{
				return;
			}

			if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
			{
				return;
			}

			if (!Helper.IsTestClass(classDeclaration, context))
			{
				return;
			}

			SymbolInfo info = context.SemanticModel.GetSymbolInfo(methodDeclaration);
			ISymbol symbol = info.Symbol;
			if (symbol is null)
			{
				return;
			}


			if (Helper.HasAnyAttribute(methodDeclaration.AttributeLists, context, attributeDefinitions))
			{
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}
