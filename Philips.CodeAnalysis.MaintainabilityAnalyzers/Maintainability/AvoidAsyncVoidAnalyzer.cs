// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAsyncVoidAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid async void";
		public const string MessageFormat = @"Methods may not have async void return type";
		private const string Description = @"To avoid unhandled exception, methods should not use async void unless a event handler.";

		public AvoidAsyncVoidAnalyzer()
			: base(DiagnosticId.AvoidAsyncVoid, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		protected override void InitializeAnalysis(CompilationStartAnalysisContext context)
		{
			if (context.Compilation.GetTypeByMetadataName(StringConstants.TaskFullyQualifiedName) == null)
			{
				return;
			}

			INamedTypeSymbol namedSymbol = context.Compilation.GetTypeByMetadataName("System.EventArgs");
			if (namedSymbol == null)
			{
				return;
			}

			context.RegisterSyntaxNodeAction((x) => Analyze(namedSymbol, x), SyntaxKind.MethodDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.SimpleLambdaExpression);
			context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.ParenthesizedLambdaExpression);
		}

		private void AnalyzeLambda(SyntaxNodeAnalysisContext context)
		{
			var lambdaExpressionSyntax = (LambdaExpressionSyntax)context.Node;

			if (lambdaExpressionSyntax.AsyncKeyword.Kind() != SyntaxKind.AsyncKeyword)
			{
				return;
			}

			SymbolInfo retValType = context.SemanticModel.GetSymbolInfo(lambdaExpressionSyntax);

			if (retValType.Symbol is null)
			{
				return;
			}

			if (retValType.Symbol is IMethodSymbol method && method.ReturnType.SpecialType == SpecialType.System_Void)
			{
				Location location = lambdaExpressionSyntax.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location));
			}
		}

		private void Analyze(INamedTypeSymbol namedSymbol, SyntaxNodeAnalysisContext context)
		{
			var methodDeclaration = (MethodDeclarationSyntax)context.Node;

			if (!methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
			{
				return;
			}

			if (methodDeclaration.ReturnType is not PredefinedTypeSyntax predefinedTypeSyntax || predefinedTypeSyntax.Keyword.Kind() != SyntaxKind.VoidKeyword)
			{
				return;
			}

			ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

			if (symbol is not IMethodSymbol methodSymbol)
			{
				return;
			}

			// check for the Event handlers as they use async void

			if (methodSymbol.Parameters.Any(x => x.Type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsDerivedFrom(namedSymbol)))
			{
				return;
			}

			Location location = methodDeclaration.ReturnType.GetLocation();
			var displayString = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
			context.ReportDiagnostic(Diagnostic.Create(Rule, location, displayString));
		}


	}
}
