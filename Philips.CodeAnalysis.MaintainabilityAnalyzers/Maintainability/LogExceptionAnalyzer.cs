// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

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
		private const string Title = "Log caught exceptions.";
		private const string Message = "Exception that is caught, is not logged.";
		private const string Description = "Log caught exceptions.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.LogException),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: false,
				description: Description
			);

		private string _logMethodNames;

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
					var methodNames = additionalFiles.GetValueFromEditorConfig(Rule.Id, "log_method_names");
					if (string.IsNullOrEmpty(methodNames))
					{
						startContext.RegisterCompilationEndAction(ReportParsingError);
					}
					else
					{
						_logMethodNames = methodNames;
						startContext.RegisterSyntaxNodeAction(AnalyzeCatchException, SyntaxKind.CatchClause);
					}
				});
		}

		private void AnalyzeCatchException(SyntaxNodeAnalysisContext context)
		{
			var catchNode = (CatchClauseSyntax)context.Node;
			// Look for logging method calls underneath this node.
			var hasCallingLogNodes = catchNode.DescendantNodes()
				.OfType<InvocationExpressionSyntax>().Any(x => IsCallingLogMethod(context, x));
			// If another exception is thrown, logging is not required.
			var hasThrowNodes = catchNode.DescendantNodes().OfType<ThrowStatementSyntax>().Any();
			if (!hasCallingLogNodes && !hasThrowNodes)
			{
				var location = catchNode.CatchKeyword.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location));
			}
		}

		private void ReportParsingError(CompilationAnalysisContext context)
		{
			var loc = Location.Create(context.Compilation.SyntaxTrees.First(), TextSpan.FromBounds(0, 0));
			context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
		}

		private bool IsCallingLogMethod(SyntaxNodeAnalysisContext context, SyntaxNode node)
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
