// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Report when a catch exception block does not call one of the logging methods.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LogExceptionAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = "Log caught exceptions.";
		private const string Message = "Exception caught in line {0} is not logged.";
		private const string Description = "Log caught exceptions.";
		private const string Category = Categories.RuntimeFailure;

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.LogException),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: Description
			);

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
			context.RegisterSyntaxNodeAction(AnalyzeCatchException, SyntaxKind.CatchClause);
		}

		private void AnalyzeCatchException(SyntaxNodeAnalysisContext context)
		{
			var catchNode = context.Node;
			// Look for logging method calls underneath this node.
			var hasCallingLogNodes = catchNode.DescendantNodes().OfType<InvocationExpressionSyntax>()
				.Where(x => IsCallingLogMethod(context, x)).Any();
			// If another exception is thrown, logging is not required.
			var hasThrowNodes = catchNode.DescendantNodes().OfType<ThrowStatementSyntax>().Any();
			if (!hasCallingLogNodes && !hasThrowNodes)
			{
				var location = catchNode.GetLocation();
				var lineNum = Helper.GetLineNumber(location);
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, lineNum));
			}
		}

		private bool IsCallingLogMethod(SyntaxNodeAnalysisContext context, SyntaxNode node)
		{
			var additionalFilesHelper = new AdditionalFilesHelper(context.Options, context.Compilation);
			var logMethodNames = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"log_method_names");
			var isLoggingMethod = false;
			var invocation = (InvocationExpressionSyntax)node;
			if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				var methodName = memberAccess.Name.Identifier.Text;
				isLoggingMethod = logMethodNames.Contains(methodName);
			}
			return isLoggingMethod;
		}
	}
}
