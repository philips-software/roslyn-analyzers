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

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.TestClassPublicMethodShouldBeTestMethod), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

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

				MsTestAttributeDefinitions definitions = MsTestAttributeDefinitions.FromCompilation(startContext.Compilation);

				startContext.RegisterSyntaxNodeAction((x) => Analyze(definitions, x), SyntaxKind.MethodDeclaration);
			});
		}

		private void Analyze(MsTestAttributeDefinitions definitions, SyntaxNodeAnalysisContext context)
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

			ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

			if (symbol is null)
			{
				return;
			}

			bool isAllowedToBePublic = false;
			foreach (AttributeData attribute in symbol.GetAttributes())
			{
				if (definitions.NonTestMethods.Contains(attribute.AttributeClass))
				{
					isAllowedToBePublic = true;
					break;
				}

				if (attribute.AttributeClass.IsDerivedFrom(definitions.TestMethodSymbol))
				{
					isAllowedToBePublic = true;
					break;
				}
			}

			if (!isAllowedToBePublic)
			{
				var location = methodDeclaration.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(Rule, location);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
