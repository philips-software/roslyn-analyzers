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
		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidStaticClasses), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public virtual AvoidStaticClassesCompilationAnalyzer CreateCompilationAnalyzer(HashSet<string> exceptions, bool generateExceptionsFile)
		{
			return new AvoidStaticClassesCompilationAnalyzer(exceptions, generateExceptionsFile);
		}

		public virtual void Register(CompilationStartAnalysisContext compilationContext)
		{
			AdditionalFilesHelper helper = new AdditionalFilesHelper(compilationContext.Options, compilationContext.Compilation);
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
			if (Helper.IsGeneratedCode(context))
			{
				return;
			}

			ClassDeclarationSyntax classDeclarationSyntax = context.Node as ClassDeclarationSyntax;
			if (classDeclarationSyntax == null)
			{
				return;
			}

			// Only interested in static class declarations
			if (!classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				return;
			}

			// We need to let it go if it's white-listed (i.e., legacy)
			if (_exceptions.Any(str => str.EndsWith(@"." + classDeclarationSyntax.Identifier.ValueText)))
			{
				// We probably have a whitelisted class.  Let's confirm.
				if (_exceptions.Contains(context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax).ToDisplayString()))
				{
					return;
				}
			}

			// Even more likely to have a whitelisted class, with the wildcard on the namespace
			if (_exceptions.Any(str => str.EndsWith(@"*." + classDeclarationSyntax.Identifier.ValueText)))
			{
				return;
			}

			// Check if this is an extension class
			var model = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
			if (model is { MightContainExtensionMethods: true })
			{
				if (!model.GetMembers().Any(m =>
											m.Kind == SymbolKind.Method &&
											m.DeclaredAccessibility == Accessibility.Public &&
											!((IMethodSymbol)m).IsExtensionMethod))
				{
					return;
				}
			}

			if (_generateExceptionsFile)
			{
				File.AppendAllText(@"StaticClasses.Allowed.GENERATED.txt", context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax).ToDisplayString() + Environment.NewLine);
			}

			Diagnostic diagnostic = Diagnostic.Create(AvoidStaticClassesAnalyzer.Rule, classDeclarationSyntax.Modifiers.First(t => t.Kind() == SyntaxKind.StaticKeyword).GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}
