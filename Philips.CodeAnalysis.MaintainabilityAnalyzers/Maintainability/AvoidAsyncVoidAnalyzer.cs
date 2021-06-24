// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAsyncVoidAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid async void";
		public const string MessageFormat = @"Methods may not have async void return type";
		private const string Description = @"To avoid unhandled exception, methods should not use async void unless a event handler.";
		private const string Category = Categories.Maintainability;
		private const string HelpUri = @"https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming#avoid-async-void";
		
		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidAsyncVoid), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description, helpLinkUri: HelpUri);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task") == null)
				{
					return;
				}
				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
			});
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)context.Node;


			if (!methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword) ||
				methodDeclaration.ReturnType.ToString() != "void")
			{
				return;
			}

			ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

			if (symbol is null || !(symbol is IMethodSymbol methodSymbol))
			{
				return;
			}

			// check for the Event handlers as they use async void
			INamedTypeSymbol namedSymbol = context.Compilation.GetTypeByMetadataName("System.EventArgs");
			if (methodSymbol.Parameters.Any(x => (x.Type as INamedTypeSymbol).IsDerivedFrom(namedSymbol)))
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.ReturnType.GetLocation(), methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
		}

		
	}
}
