// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidVariableNamedUnderscoreAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid unnecessary typed discards and variables named '_'";
		private const string MessageFormat = @"Use anonymous discard '_' instead of typed discard or variable named '{0}' when type is not needed";
		private const string Description = @"Prefer anonymous discards over typed discards when the type is not needed for overload resolution, and avoid variables named exactly '_' which can be confused with discards.";

		public AvoidVariableNamedUnderscoreAnalyzer()
			: base(DiagnosticId.AvoidVariableNamedUnderscore, Title, MessageFormat, Description, Categories.Naming)
		{
		}

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
			context.RegisterSyntaxNodeAction(AnalyzeArgument, SyntaxKind.Argument);
		}

		private void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var foreachStatement = (ForEachStatementSyntax)context.Node;

			if (foreachStatement.Identifier.ValueText != "_")
			{
				return;
			}

			Location location = foreachStatement.GetLocation();
			var diagnostic = Diagnostic.Create(Rule, location, foreachStatement.Identifier.ValueText);
			context.ReportDiagnostic(diagnostic);
		}

		private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var variableDeclaration = (VariableDeclarationSyntax)context.Node;
			if (variableDeclaration.Parent is not (ForStatementSyntax or UsingStatementSyntax or LocalDeclarationStatementSyntax))
			{
				return;
			}

			foreach (var identifier in variableDeclaration.Variables.Select(variable => variable.Identifier.ValueText))
			{
				if (identifier != "_")
				{
					continue;
				}

				Location location = variableDeclaration.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location, identifier);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private void AnalyzeArgument(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var argument = (ArgumentSyntax)context.Node;

			// Only check out arguments
			if (!argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
			{
				return;
			}

			// Check if it's a typed discard (e.g., out int _)
			if (argument.Expression is DeclarationExpressionSyntax declaration &&
				declaration.Designation is DiscardDesignationSyntax discard)
			{
				// This is a typed discard (e.g., out int _)
				// Only flag if the type is not needed for overload resolution
				if (!IsTypedDiscardNecessaryForOverloadResolution(context, argument))
				{
					Location location = discard.GetLocation();
					var diagnostic = Diagnostic.Create(Rule, location, "_");
					context.ReportDiagnostic(diagnostic);
				}
				return;
			}

			// Check if it's a variable declaration with identifier "_"
			if (argument.Expression is DeclarationExpressionSyntax declaration2 &&
				declaration2.Designation is SingleVariableDesignationSyntax variable &&
				variable.Identifier.ValueText == "_")
			{
				Location location = variable.Identifier.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location, variable.Identifier.ValueText);
				context.ReportDiagnostic(diagnostic);
				return;
			}

			// Check if it's a simple identifier named "_" (for original functionality)
			if (argument.Expression is IdentifierNameSyntax identifier &&
				identifier.Identifier.ValueText == "_")
			{
				Location location = identifier.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location, identifier.Identifier.ValueText);
				context.ReportDiagnostic(diagnostic);
			}

			// Note: We don't flag DiscardDesignationSyntax (anonymous discards like "out _")
			// when they are IdentifierNameSyntax, as they are the preferred form
		}

		private bool IsTypedDiscardNecessaryForOverloadResolution(SyntaxNodeAnalysisContext context, ArgumentSyntax argument)
		{
			// Find the method call containing this argument
			SyntaxNode current = argument.Parent;
			while (current is not null and not InvocationExpressionSyntax)
			{
				current = current.Parent;
			}

			if (current is not InvocationExpressionSyntax invocation)
			{
				return false; // Can't find method call, allow the flag (be conservative)
			}

			// Get semantic information about the method
			SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
			if (symbolInfo.Symbol is not IMethodSymbol method)
			{
				return false; // Can't resolve method, allow the flag
			}

			// Get the argument position
			var argumentIndex = invocation.ArgumentList.Arguments.IndexOf(argument);
			if (argumentIndex < 0 || argumentIndex >= method.Parameters.Length)
			{
				return false; // Invalid position, allow the flag
			}

			// Get all methods with the same name in the same type
			INamedTypeSymbol containingType = method.ContainingType;
			System.Collections.Generic.IEnumerable<IMethodSymbol> methodsWithSameName = containingType.GetMembers(method.Name).OfType<IMethodSymbol>();

			// Check if there are multiple overloads with different out parameter types at this position
			var outParameterTypes = methodsWithSameName
				.Where(m => argumentIndex < m.Parameters.Length)
				.Where(m => m.Parameters[argumentIndex].RefKind == RefKind.Out)
				.Select(m => m.Parameters[argumentIndex].Type.ToDisplayString())
				.Distinct()
				.ToList();

			// If there are multiple different out parameter types, the typed discard is necessary
			return outParameterTypes.Count > 1;
		}
	}
}
