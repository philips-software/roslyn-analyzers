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

			CSharpSyntaxNode violation = foreachStatement;
			Location location = violation.GetLocation();
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

				CSharpSyntaxNode violation = variableDeclaration;
				Location location = violation.GetLocation();
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

					Location location = variable.Identifier.GetLocation();
					var diagnostic = Diagnostic.Create(Rule, location, variable.Identifier.ValueText);
					context.ReportDiagnostic(diagnostic);
				}
				else if (declaration.Designation is DiscardDesignationSyntax discard)
				{
					Location location = discard.UnderscoreToken.GetLocation();
					var diagnostic = Diagnostic.Create(Rule, location, discard.UnderscoreToken.ValueText);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}