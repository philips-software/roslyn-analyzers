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
		private const string InvalidSetupMessage = @"This analyzer requires an .editorconfig entry of the form dotnet_code_quality.{0}.{1} specifying a comma-separated list of allowed method calls inside catch blocks.";
		private const string InvalidSetupDescription = @"This analyzer requires additional configuration in the .editorconfig.";
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
				startContext =>
				{
					var additionalFiles = new AdditionalFilesHelper(
						startContext.Options,
						startContext.Compilation);
					var methodNames = additionalFiles.GetValuesFromEditorConfig(Rule.Id, LogMethodNames);

					var compilationAnalyzer = new CompilationAnalyzer(methodNames);

					if (methodNames.Count == 0)
					{
						startContext.RegisterCompilationEndAction(compilationAnalyzer.ReportParsingError);
					}
					else
					{
						startContext.RegisterSyntaxNodeAction(compilationAnalyzer.AnalyzeCatchException, SyntaxKind.CatchClause);
					}
				});
		}

		private sealed class CompilationAnalyzer
		{
			private readonly IEnumerable<string> _logMethodNames;
			public CompilationAnalyzer(IEnumerable<string> logMethodNames)
			{
				_logMethodNames = logMethodNames;
			}


			public void AnalyzeCatchException(SyntaxNodeAnalysisContext context)
			{
				var catchNode = (CatchClauseSyntax)context.Node;
				// Look for logging method calls underneath this node.
				var hasCallingLogNodes = catchNode.DescendantNodes()
					.OfType<InvocationExpressionSyntax>()
					.Any(x => IsCallingLogMethod(x));
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
				context.ReportDiagnostic(Diagnostic.Create(InvalidSetupRule, loc, Rule.Id, LogMethodNames));
			}

			private bool IsCallingLogMethod(SyntaxNode node)
			{
				var isLoggingMethod = false;
				var invocation = (InvocationExpressionSyntax)node;
				if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					var methodName = memberAccess.Name.Identifier.Text;
					isLoggingMethod = _logMethodNames.Contains(methodName);
				}
				return isLoggingMethod;
			}
		}
	}
}
