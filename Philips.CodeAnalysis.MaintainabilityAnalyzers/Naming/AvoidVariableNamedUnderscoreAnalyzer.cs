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
		private const string Title = @"Avoid variables named exactly '_'";
		private const string MessageFormat = @"Variable '{0}' is named '_' which can be confused with a discard. Consider using a discard or a more descriptive variable name";
		private const string Description = @"Variables named exactly '_' can be confused with C# discards. Use either a proper discard or a more descriptive variable name.";

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

			foreach (SyntaxToken identifier in variableDeclaration.Variables.Select(variable => variable.Identifier))
			{
				if (identifier.ValueText != "_")
				{
					continue;
				}

				Location location = variableDeclaration.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location, identifier.ValueText);
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

			// Check if it's a variable declaration
			if (argument.Expression is DeclarationExpressionSyntax declaration)
			{
				if (declaration.Designation is SingleVariableDesignationSyntax variable)
				{
					if (variable.Identifier.ValueText != "_")
					{
						return;
					}

					// This is a typed discard (e.g., out int _)
					// Only flag if the type is not needed for overload resolution
					if (!IsTypedDiscardNecessaryForOverloadResolution(context, argument))
					{
						Location location = variable.Identifier.GetLocation();
						var diagnostic = Diagnostic.Create(Rule, location, variable.Identifier.ValueText);
						context.ReportDiagnostic(diagnostic);
					}
				}
				// Note: We don't flag DiscardDesignationSyntax (anonymous discards like "out _")
				// as they are the preferred form and should not be flagged
			}
		}

		private bool IsTypedDiscardNecessaryForOverloadResolution(SyntaxNodeAnalysisContext context, ArgumentSyntax argument)
		{
			// Find the method call containing this argument
			var current = argument.Parent;
			while (current != null && current is not InvocationExpressionSyntax)
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
			int argumentIndex = invocation.ArgumentList.Arguments.IndexOf(argument);
			if (argumentIndex < 0 || argumentIndex >= method.Parameters.Length)
			{
				return false; // Invalid position, allow the flag
			}

			// Get all methods with the same name in the same type
			var containingType = method.ContainingType;
			var methodsWithSameName = containingType.GetMembers(method.Name).OfType<IMethodSymbol>();

			// Check if there are multiple overloads with different out parameter types at this position
			var outParameterTypes = methodsWithSameName
				.Where(m => argumentIndex < m.Parameters.Length)
				.Where(m => m.Parameters[argumentIndex].RefKind == RefKind.Out)
				.Select(m => m.Parameters[argumentIndex].Type.ToDisplayString())
				.Distinct()
				.ToList();

			// If there are multiple different out parameter types, the typed discard might be necessary
			return outParameterTypes.Count > 1;
		}
	}
}