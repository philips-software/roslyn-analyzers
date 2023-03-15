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
	public class AvoidStaticClassesAnalyzer : DiagnosticAnalyzer
	{
		public const string AllowedFileName = @"StaticClasses.Allowed.txt";
		private const string Title = @"Avoid static classes";
		public const string MessageFormat = Title;
		private const string Description = @"Static Classes are not easily mockable. Avoid them so that your code is Unit Testable.";
		private const string Category = Categories.Maintainability;
		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidStaticClasses), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public virtual AvoidStaticClassesCompilationAnalyzer CreateCompilationAnalyzer(AllowedSymbols allowedSymbols, bool shouldGenerateExceptionsFile)
		{
			return new AvoidStaticClassesCompilationAnalyzer(allowedSymbols, shouldGenerateExceptionsFile);
		}

		public virtual void Register(CompilationStartAnalysisContext compilationContext)
		{
			AllowedSymbols allowedSymbols = new(compilationContext.Compilation);
			_ = allowedSymbols.Initialize(compilationContext.Options.AdditionalFiles, AllowedFileName);
			// Add standard exceptions
			allowedSymbols.RegisterLine(@"*.Startup");
			allowedSymbols.RegisterLine(@"*.Program");
			allowedSymbols.RegisterLine(@"*.AssemblyInitialize");

			AdditionalFilesHelper helper = new(compilationContext.Options, compilationContext.Compilation);
			ExceptionsOptions exceptionsOptions = helper.LoadExceptionsOptions(Rule.Id);
			AvoidStaticClassesCompilationAnalyzer compilationAnalyzer = CreateCompilationAnalyzer(allowedSymbols, exceptionsOptions.ShouldGenerateExceptionsFile);
			compilationContext.RegisterSyntaxNodeAction(compilationAnalyzer.Analyze, SyntaxKind.ClassDeclaration);
		}

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterCompilationStartAction(Register);
		}
	}

	public class AvoidStaticClassesCompilationAnalyzer
	{
		private readonly AllowedSymbols _allowedSymbols;
		private readonly bool _shouldGenerateExceptionsFile;

		public AvoidStaticClassesCompilationAnalyzer(AllowedSymbols allowedSymbols, bool shouldGenerateExceptionsFile)
		{
			_allowedSymbols = allowedSymbols;
			_shouldGenerateExceptionsFile = shouldGenerateExceptionsFile;
		}

		public void Analyze(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
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

			// If the class only contains const and static readonly fields, let it go
			if (!classDeclarationSyntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Any() &&
				classDeclarationSyntax.DescendantNodes().OfType<FieldDeclarationSyntax>().All(IsConstant))
			{
				return;
			}

			INamedTypeSymbol declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

			// We need to let it go if it's white-listed (i.e., legacy)
			if (_allowedSymbols.IsAllowed(declaredSymbol))
			{
				return;
			}

			Helper helper = new();
			if (helper.IsExtensionClass(declaredSymbol))
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

		private static bool IsConstant(FieldDeclarationSyntax field)
		{
			SyntaxTokenList modifiers = field.Modifiers;
			return modifiers.Any(SyntaxKind.ConstKeyword) ||
				   (modifiers.Any(SyntaxKind.StaticKeyword) && modifiers.Any(SyntaxKind.ReadOnlyKeyword));
		}
	}
}
