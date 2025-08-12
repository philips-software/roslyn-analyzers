// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidStaticClassesAnalyzer : DiagnosticAnalyzerBase
	{
		public const string AllowedFileName = @"StaticClasses.Allowed.txt";
		private const string Title = @"Avoid static classes";
		public const string MessageFormat = Title;
		private const string Description = @"Static Classes are not easily mockable. Avoid them so that your code is Unit Testable.";
		private const string Category = Categories.Maintainability;
		public static readonly DiagnosticDescriptor Rule = new(DiagnosticId.AvoidStaticClasses.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public virtual AvoidStaticClassesCompilationAnalyzer CreateCompilationAnalyzer(Helper helper, bool shouldGenerateExceptionsFile)
		{
			var analyzer = new AvoidStaticClassesCompilationAnalyzer(helper, shouldGenerateExceptionsFile);
			return analyzer;
		}

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			_ = Helper.ForAllowedSymbols.Initialize(context.Options.AdditionalFiles, AllowedFileName);

			// Add standard exceptions
			Helper.ForAllowedSymbols.RegisterLine(@"*.Startup");
			Helper.ForAllowedSymbols.RegisterLine(@"*.Program");
			Helper.ForAllowedSymbols.RegisterLine(@"*.AssemblyInitialize");

			ExceptionsOptions exceptionsOptions = Helper.ForAdditionalFiles.LoadExceptionsOptions(Rule.Id);
			AvoidStaticClassesCompilationAnalyzer compilationAnalyzer = CreateCompilationAnalyzer(Helper, exceptionsOptions.ShouldGenerateExceptionsFile);
			context.RegisterSyntaxNodeAction(compilationAnalyzer.Analyze, SyntaxKind.ClassDeclaration);
		}
	}

	public class AvoidStaticClassesCompilationAnalyzer
	{
		private readonly bool _shouldGenerateExceptionsFile;
		private readonly Helper _helper;

		public AvoidStaticClassesCompilationAnalyzer(Helper helper, bool shouldGenerateExceptionsFile)
		{
			_shouldGenerateExceptionsFile = shouldGenerateExceptionsFile;
			_helper = helper;
		}

		public void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (_helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
			{
				return;
			}

			// Only interested in static class declarations
			if (!classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				return;
			}

			// If the class only contains const and static readonly fields, and no executable members, let it go
			if (OnlyContainsConstantFieldsAndNoExecutableMembers(classDeclarationSyntax))
			{
				return;
			}

			INamedTypeSymbol declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

			// We need to let it go if it's white-listed (i.e., legacy)
			if (_helper.ForAllowedSymbols.IsAllowed(declaredSymbol))
			{
				return;
			}

			if (_helper.ForTypes.IsExtensionClass(declaredSymbol))
			{
				return;
			}

			if (_shouldGenerateExceptionsFile)
			{
				INamedTypeSymbol exceptionSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
				if (exceptionSymbol != null)
				{
					var docId = exceptionSymbol.GetDocumentationCommentId();
					File.AppendAllText(@"StaticClasses.Allowed.GENERATED.txt", $"~{docId}{Environment.NewLine}");
				}
			}

			Location location = classDeclarationSyntax.Modifiers.First(t => t.Kind() == SyntaxKind.StaticKeyword).GetLocation();
			var diagnostic = Diagnostic.Create(AvoidStaticClassesAnalyzer.Rule, location);
			context.ReportDiagnostic(diagnostic);
		}

		private static bool OnlyContainsConstantFieldsAndNoExecutableMembers(ClassDeclarationSyntax classDeclarationSyntax)
		{
			// Check if there are any executable members (methods, properties, constructors, events, indexers)
			if (classDeclarationSyntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Any() ||
				classDeclarationSyntax.DescendantNodes().OfType<PropertyDeclarationSyntax>().Any() ||
				classDeclarationSyntax.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Any() ||
				classDeclarationSyntax.DescendantNodes().OfType<EventDeclarationSyntax>().Any() ||
				classDeclarationSyntax.DescendantNodes().OfType<EventFieldDeclarationSyntax>().Any() ||
				classDeclarationSyntax.DescendantNodes().OfType<IndexerDeclarationSyntax>().Any())
			{
				return false;
			}

			// Check if all fields are constant (const or static readonly)
			return classDeclarationSyntax.DescendantNodes().OfType<FieldDeclarationSyntax>().All(IsConstant);
		}

		private static bool IsConstant(FieldDeclarationSyntax field)
		{
			SyntaxTokenList modifiers = field.Modifiers;
			return modifiers.Any(SyntaxKind.ConstKeyword) ||
				   (modifiers.Any(SyntaxKind.StaticKeyword) && modifiers.Any(SyntaxKind.ReadOnlyKeyword));
		}
	}
}
