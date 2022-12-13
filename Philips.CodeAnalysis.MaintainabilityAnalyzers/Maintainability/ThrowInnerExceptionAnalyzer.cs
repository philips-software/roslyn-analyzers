// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Include the original exception when rethrowing.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ThrowInnerExceptionAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = "Use inner exceptions for unhandled exceptions";
		private const string Message = "Rethrown exception should include caught exception.";
		private const string Description = "Use inner exceptions for unhandled exceptions";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule =
			new(
				Helper.ToDiagnosticId(DiagnosticIds.ThrowInnerException),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: false,
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
			context.RegisterSyntaxNodeAction(
				AnalyzeCatchException,
				SyntaxKind.CatchClause
			);
		}

		private void AnalyzeCatchException(SyntaxNodeAnalysisContext context)
		{
			var catchNode = (CatchClauseSyntax)context.Node;
			// Look for throw statements and check them.
			var hasBadThrowNodes = catchNode.DescendantNodes()
				.OfType<ThrowStatementSyntax>().Any(node => !IsCorrectThrow(context, node));
			if (hasBadThrowNodes)
			{
				var location = catchNode.CatchKeyword.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location));
			}
		}

		// Throw should rethrow same exception, or include original exception
		// when creating new Exception.
		// Alternatively, also allow the HttpResponseException method using in ASP .NET Core.
		private bool IsCorrectThrow(SyntaxNodeAnalysisContext context, ThrowStatementSyntax node)
		{
			bool isOk = true;
			var newNodes = node.ChildNodes().OfType<ObjectCreationExpressionSyntax>();
			if (newNodes.Any())
			{
				foreach (var creation in newNodes)
				{
					// Constructor needs to have at least two arguments.
					isOk = creation.ArgumentList != null && creation.ArgumentList.Arguments.Count > 1;
					if (!isOk)
					{
						// The HttpResponseException has only a single argument.
						var typeSymbol = context.SemanticModel.GetTypeInfo(creation).Type;
						if (typeSymbol.Name == "HttpResponseException")
						{
							isOk = true;
							break;
						}
					}
				}
			}
			return isOk;
		}
	}
}
