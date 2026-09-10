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
	public class AvoidVariableNamedUnderscoreAnalyzer : DiagnosticAnalyzerBase
	{
		private const string VariableNamedUnderscoreTitle = @"Avoid variables named '_'";
		private const string VariableNamedUnderscoreMessageFormat = @"Avoid variable named '{0}' as it can be confused with discards";
		private const string VariableNamedUnderscoreDescription = @"Avoid variables named exactly '_' which can be confused with discards.";

		private const string UnnecessaryTypedDiscardTitle = @"Avoid unnecessary typed discard";
		private const string UnnecessaryTypedDiscardMessageFormat = @"Use anonymous discard '_' instead of typed discard '{0}' when type is not needed";
		private const string UnnecessaryTypedDiscardDescription = @"Prefer anonymous discards over typed discards when the type is not needed for overload resolution.";

		public static readonly DiagnosticDescriptor VariableNamedUnderscoreRule = new(
			DiagnosticId.AvoidVariableNamedUnderscore.ToId(),
			VariableNamedUnderscoreTitle,
			VariableNamedUnderscoreMessageFormat,
			Categories.Naming,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: VariableNamedUnderscoreDescription,
			helpLinkUri: DiagnosticId.AvoidVariableNamedUnderscore.ToHelpLinkUrl());

		public static readonly DiagnosticDescriptor UnnecessaryTypedDiscardRule = new(
			DiagnosticId.AvoidUnnecessaryTypedDiscard.ToId(),
			UnnecessaryTypedDiscardTitle,
			UnnecessaryTypedDiscardMessageFormat,
			Categories.Naming,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: UnnecessaryTypedDiscardDescription,
			helpLinkUri: DiagnosticId.AvoidUnnecessaryTypedDiscard.ToHelpLinkUrl());

		public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			System.Collections.Immutable.ImmutableArray.Create(VariableNamedUnderscoreRule, UnnecessaryTypedDiscardRule);

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
			var diagnostic = Diagnostic.Create(VariableNamedUnderscoreRule, location, foreachStatement.Identifier.ValueText);
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
				var diagnostic = Diagnostic.Create(VariableNamedUnderscoreRule, location, identifier);
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
					var diagnostic = Diagnostic.Create(UnnecessaryTypedDiscardRule, location, "_");
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
				var diagnostic = Diagnostic.Create(VariableNamedUnderscoreRule, location, variable.Identifier.ValueText);
				context.ReportDiagnostic(diagnostic);
			}

			// Note: We don't flag IdentifierNameSyntax("_") because that represents
			// anonymous discards like "out _" which are the preferred form
		}

		private static bool IsTypedDiscardNecessaryForOverloadResolution(SyntaxNodeAnalysisContext context, ArgumentSyntax argument)
		{
			// Find the method call containing this argument
			InvocationExpressionSyntax invocation = argument.Ancestors()
				.OfType<InvocationExpressionSyntax>()
				.FirstOrDefault();
			if (invocation is null)
			{
				return false; // Can't find method call, allow the flag
			}

			// Early check: if the invocation doesn't look like it could have overloads, 
			// we can avoid semantic model access entirely
			if (invocation.Expression is not (MemberAccessExpressionSyntax or IdentifierNameSyntax))
			{
				return false; // Complex expression, allow the flag
			}

			// Get semantic information about the method - moved lower for performance
			SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
			if (symbolInfo.Symbol is not IMethodSymbol method)
			{
				return false; // Can't resolve method, allow the flag
			}

			// Get the argument position - handle named arguments
			var argumentIndex = GetArgumentIndex(invocation.ArgumentList.Arguments, argument, method);
			if (argumentIndex < 0 || argumentIndex >= method.Parameters.Length)
			{
				return false; // Invalid position, allow the flag
			}

			// Simple heuristic: check if there are method overloads with different out parameter types at this position
			INamedTypeSymbol containingType = method.ContainingType;
			System.Collections.Generic.IEnumerable<IMethodSymbol> methodsWithSameName = containingType.GetMembers(method.Name).OfType<IMethodSymbol>();

			// Count how many overloads have an out parameter at this position
			var overloadCount = methodsWithSameName
				.Where(m => argumentIndex < m.Parameters.Length)
				.Count(m => m.Parameters[argumentIndex].RefKind == RefKind.Out);

			// If there are multiple overloads with out parameters at this position,
			// then the typed discard might be necessary for overload resolution
			return overloadCount > 1;
		}

		private static int GetArgumentIndex(SeparatedSyntaxList<ArgumentSyntax> arguments, ArgumentSyntax targetArgument, IMethodSymbol method)
		{
			// Handle named arguments by checking if the target argument has a name
			if (targetArgument.NameColon != null)
			{
				var parameterName = targetArgument.NameColon.Name.Identifier.ValueText;
				for (var i = 0; i < method.Parameters.Length; i++)
				{
					if (method.Parameters[i].Name == parameterName)
					{
						return i;
					}
				}
				return -1; // Parameter name not found
			}

			// For positional arguments, just get the index
			return arguments.IndexOf(targetArgument);
		}
	}
}
