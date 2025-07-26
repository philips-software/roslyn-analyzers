// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidVariablesNamedUnderscoreAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid variables named exactly '_'";
		private const string MessageFormat = @"Variable '{0}' is named exactly '_' which can be confused with discards";
		private const string Description = @"Variables named exactly '_' (single underscore) can be confused with C# discards and should be avoided.";

		public AvoidVariablesNamedUnderscoreAnalyzer()
			: base(DiagnosticId.AvoidVariablesNamedUnderscore, Title, MessageFormat, Description, Categories.Naming)
		{
		}

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
		}

		private void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var foreachStatement = (ForEachStatementSyntax)context.Node;

			if (IsNamedUnderscore(foreachStatement.Identifier))
			{
				Location location = foreachStatement.Identifier.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location, foreachStatement.Identifier.ValueText);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var variableDeclaration = (VariableDeclarationSyntax)context.Node;

			foreach (SyntaxToken identifier in variableDeclaration.Variables.Select(variable => variable.Identifier))
			{
				if (IsNamedUnderscore(identifier))
				{
					Location location = identifier.GetLocation();
					var diagnostic = Diagnostic.Create(Rule, location, identifier.ValueText);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		private static bool IsNamedUnderscore(SyntaxToken identifier)
		{
			return identifier.ValueText == "_";
		}
	}
}