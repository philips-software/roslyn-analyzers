// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Report when a catch exception block does not call one of the logging methods.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LogExceptionAnalyzer : DiagnosticAnalyzer
	{
		public const string AllowedFileName = "AllowedLogMethods.txt";
		private const string LogMethodNames = "log_method_names";

		private const string Title = "Log caught exceptions.";
		private const string Message = "Exception that is caught is not logged.";
		private const string Description = "Log caught exceptions.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule =
			new(
				Helper.ToDiagnosticId(DiagnosticId.LogException),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: false,
				description: Description
			);

		private const string InvalidSetupTitle = @"Log caught exceptions setup";
		private const string InvalidSetupMessage = @"This analyzer requires an either .editorconfig entry of the form dotnet_code_quality.{0}.{1} specifying a comma-separated list or an <AdditionalFiles> element named {2} specifying a list of allowed method calls inside catch blocks.";
		private const string InvalidSetupDescription = @"This analyzer requires additional configuration in the .editorconfig or <AdditionalFiles> element.";
		private static readonly DiagnosticDescriptor InvalidSetupRule = new(Helper.ToDiagnosticId(DiagnosticId.LogException), InvalidSetupTitle, InvalidSetupMessage, Category, DiagnosticSeverity.Error, false, InvalidSetupDescription);


		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, InvalidSetupRule);

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(
				compilationContext =>
				{
					var allowedSymbols = new AllowedSymbols(compilationContext.Compilation);
					allowedSymbols.RegisterLine("*.Log.*");
					allowedSymbols.Initialize(compilationContext.Options.AdditionalFiles, AllowedFileName);

					// Support legacy configuration via .editorconfig also.
					var additionalFiles = new AdditionalFilesHelper(
						compilationContext.Options,
						compilationContext.Compilation);
					var methodNames = additionalFiles.GetValuesFromEditorConfig(Rule.Id, LogMethodNames);
					foreach (var methodName in methodNames)
					{
						allowedSymbols.RegisterLine(methodName);
					}

					var compilationAnalyzer = new CompilationAnalyzer(allowedSymbols);

					if (allowedSymbols.Count == 0)
					{
						compilationContext.RegisterCompilationEndAction(compilationAnalyzer.ReportParsingError);
					}
					else
					{
						compilationContext.RegisterSyntaxNodeAction(compilationAnalyzer.AnalyzeCatchException, SyntaxKind.CatchClause);
					}
				});
		}

		private sealed class CompilationAnalyzer
		{
			private readonly AllowedSymbols _logMethodNames;
			
			public CompilationAnalyzer(AllowedSymbols logMethodNames)
			{
				_logMethodNames = logMethodNames;
			}


			public void AnalyzeCatchException(SyntaxNodeAnalysisContext context)
			{
				var catchNode = (CatchClauseSyntax)context.Node;
				// Look for logging method calls underneath this node.
				var hasCallingLogNodes = catchNode.DescendantNodes()
					.OfType<InvocationExpressionSyntax>()
					.Any(invocation => IsCallingLogMethod(context, invocation));
				// If another exception is thrown, logging is not required.
				var hasThrowNodes = catchNode.DescendantNodes()
					.OfType<ThrowStatementSyntax>()
					.Any();
				if (!hasCallingLogNodes && !hasThrowNodes)
				{
					var location = catchNode.CatchKeyword.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, location));
				}
			}

			public void ReportParsingError(CompilationAnalysisContext context)
			{
				var syntaxTree = context.Compilation.SyntaxTrees.First();
				var loc = Location.Create(syntaxTree, TextSpan.FromBounds(0, 0));
				context.ReportDiagnostic(Diagnostic.Create(InvalidSetupRule, loc, Rule.Id, LogMethodNames, AllowedFileName));
			}

			private bool IsCallingLogMethod(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
			{
				var isLoggingMethod = false;
				if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
				    context.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol is INamedTypeSymbol typeSymbol)
				{
					isLoggingMethod = typeSymbol.GetMembers(memberAccess.Name.Identifier.Text).OfType<IMethodSymbol>().Any(method => _logMethodNames.IsAllowed(method));
				}

				return isLoggingMethod;
			}
		}
	}
}
