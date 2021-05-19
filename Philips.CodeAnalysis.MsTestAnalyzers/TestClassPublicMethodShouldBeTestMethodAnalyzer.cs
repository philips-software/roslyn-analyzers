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

		private class AttributeDefinitions
		{
			public INamedTypeSymbol TestMethodSymbol { get; set; }

			public ImmutableArray<INamedTypeSymbol> OtherAttributes { get; set; }
		}

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.TestClassPublicMethodShouldBeTestMethod), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

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

				var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

				foreach (var definition in new[]
				{
					MsTestFrameworkDefinitions.AssemblyInitializeAttribute,
					MsTestFrameworkDefinitions.AssemblyCleanupAttribute,
					MsTestFrameworkDefinitions.TestInitializeAttribute,
					MsTestFrameworkDefinitions.TestCleanupAttribute,
					MsTestFrameworkDefinitions.ClassCleanupAttribute,
					MsTestFrameworkDefinitions.ClassInitializeAttribute,
				})
				{
					var type = startContext.Compilation.GetTypeByMetadataName(definition.FullName);

					if (type is null)
					{
						continue;
					}

					builder.Add(type);
				}

				AttributeDefinitions definitions = new AttributeDefinitions()
				{
					TestMethodSymbol = startContext.Compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestMethodAttribute.FullName),
					OtherAttributes = builder.ToImmutable(),
				};

				startContext.RegisterSyntaxNodeAction((x) => Analyze(definitions, x), SyntaxKind.MethodDeclaration);
			});
		}

		private void Analyze(AttributeDefinitions definitions, SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)context.Node;

			if (!methodDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
			{
				return;
			}

			if (!(methodDeclaration.Parent is ClassDeclarationSyntax classDeclaration))
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
				if (definitions.OtherAttributes.Contains(attribute.AttributeClass))
				{
					isAllowedToBePublic = true;
					break;
				}

				if (IsDerivedFrom(attribute.AttributeClass, definitions.TestMethodSymbol))
				{
					isAllowedToBePublic = true;
					break;
				}
			}

			if (!isAllowedToBePublic)
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}

		private bool IsDerivedFrom(INamedTypeSymbol cls, INamedTypeSymbol baseClass)
		{
			while (cls != null)
			{
				if (SymbolEqualityComparer.Default.Equals(cls, baseClass))
				{
					return true;
				}

				cls = cls.BaseType;
			}

			return false;
		}
	}
}
