// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
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
		public const string FileName = @"StaticClasses.Allowed.txt";
		private const string Title = @"Avoid static classes";
		public const string MessageFormat = @"Avoid static classes";
		private const string Description = @"Static Classes are not easily mockable. Avoid them so that your code is Unit Testable.";
		private const string Category = Categories.Maintainability;
		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidStaticClasses), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public virtual AvoidStaticClassesCompilationAnalyzer CreateCompilationAnalyzer(HashSet<string> exceptions, bool generateExceptionsFile)
		{
			return new AvoidStaticClassesCompilationAnalyzer(exceptions, generateExceptionsFile);
		}

		public virtual void Register(CompilationStartAnalysisContext compilationContext)
		{
			AdditionalFilesHelper helper = new(compilationContext.Options, compilationContext.Compilation);
			HashSet<string> exceptions = helper.InitializeExceptions(FileName, Rule.Id);

			// Add standard exceptions
			exceptions.Add(@"*.Startup");
			exceptions.Add(@"*.Program");
			exceptions.Add(@"*.AssemblyInitialize");

			var compilationAnalyzer = CreateCompilationAnalyzer(exceptions, helper.ExceptionsOptions.GenerateExceptionsFile);
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
		private readonly HashSet<string> _exceptions;
		private readonly bool _generateExceptionsFile;

		public AvoidStaticClassesCompilationAnalyzer(HashSet<string> exceptions, bool generateExceptionsFile)
		{
			_exceptions = exceptions;
			_generateExceptionsFile = generateExceptionsFile;
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

			// Even more likely to have a whitelisted class with the wildcard on the namespace
			if (_exceptions.Any(str => str.EndsWith(@"*." + classDeclarationSyntax.Identifier.ValueText)))
			{
				return;
			}

			// If the class only contains const and static readonly fields, let it go
			if (!classDeclarationSyntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Any() &&
				classDeclarationSyntax.DescendantNodes().OfType<FieldDeclarationSyntax>().All(IsConstant))
			{
				return;
			}

			var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

			// We need to let it go if it's white-listed (i.e., legacy)
			var item = declaredSymbol.ToDisplayString();
			if (_exceptions.Any(str => str.EndsWith(@"." + classDeclarationSyntax.Identifier.ValueText)) &&
				_exceptions.Contains(item))
			{
				return;
			}

			Helper helper = new();
			if (helper.IsExtensionClass(declaredSymbol))
			{
				return;
			}

			if (_generateExceptionsFile)
			{
				File.AppendAllText(@"StaticClasses.Allowed.GENERATED.txt", context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax).ToDisplayString() + Environment.NewLine);
			}

			var location = classDeclarationSyntax.Modifiers.First(t => t.Kind() == SyntaxKind.StaticKeyword).GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(AvoidStaticClassesAnalyzer.Rule, location);
			context.ReportDiagnostic(diagnostic);
		}

		private static bool IsConstant(FieldDeclarationSyntax field)
		{
			var modifiers = field.Modifiers;
			return modifiers.Any(SyntaxKind.ConstKeyword) ||
			       (modifiers.Any(SyntaxKind.StaticKeyword) && modifiers.Any(SyntaxKind.ReadOnlyKeyword));
		}
	}
}
