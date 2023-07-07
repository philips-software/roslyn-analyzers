// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class VariableNamingConventionAnalyzer : SingleDiagnosticAnalyzer
	{
		private static readonly Regex _fieldRegex = new(@"^(_|[A-Z]).*$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
		private static readonly Regex _localRegex = new(@"^([a-z]|[A-Z]).*$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
		private static readonly Regex _eventRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		private const string Title = @"Follow variable naming coding guidelines";
		private const string MessageFormat = @"Rename variable '{0}' to fit coding guidelines";
		private const string Description = @"";

		private readonly bool _shouldCheckLocalVariables;
		private readonly bool _shouldCheckFieldVariables;

		public VariableNamingConventionAnalyzer() : this(true, true) { }

		public VariableNamingConventionAnalyzer(bool shouldCheckLocalVariables, bool shouldCheckFieldVariables)
			: base(DiagnosticId.VariableNamingConventions, Title, MessageFormat, Description, Categories.Naming, isEnabled: false)
		{
			_shouldCheckLocalVariables = shouldCheckLocalVariables;
			_shouldCheckFieldVariables = shouldCheckFieldVariables;
		}

		protected override void InitializeAnalysis(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
		}

		private void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new(Helper);
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			var foreachStatement = (ForEachStatementSyntax)context.Node;

			Regex validator = _localRegex;

			if (IsNameValid(validator, foreachStatement.Identifier))
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
			GeneratedCodeDetector generatedCodeDetector = new(Helper);
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			var variableDeclaration = (VariableDeclarationSyntax)context.Node;

			foreach (SyntaxToken identifier in variableDeclaration.Variables.Select(variable => variable.Identifier))
			{
				bool shouldCheck;
				Regex validator;
				switch (variableDeclaration.Parent.Kind())
				{
					case SyntaxKind.ForStatement:
					case SyntaxKind.UsingStatement:
						shouldCheck = _shouldCheckLocalVariables;
						validator = _localRegex;
						break;
					case SyntaxKind.LocalDeclarationStatement:
						{
							shouldCheck = _shouldCheckLocalVariables;
							validator = _localRegex;
							break;
						}
					case SyntaxKind.FieldDeclaration:
						{
							var fieldDeclaration = (FieldDeclarationSyntax)variableDeclaration.Parent;

							if (fieldDeclaration.Modifiers.Any(x => x.Kind() == SyntaxKind.PublicKeyword))
							{
								continue;
							}

							shouldCheck = _shouldCheckFieldVariables;

							validator = _fieldRegex;
							break;
						}
					case SyntaxKind.EventFieldDeclaration:
						shouldCheck = _shouldCheckFieldVariables;
						validator = _eventRegex;
						break;
					default:
						shouldCheck = false;
						validator = _fieldRegex;
						break;

				}

				if (!shouldCheck)
				{
					continue;
				}

				if (IsNameValid(validator, identifier))
				{
					continue;
				}

				CSharpSyntaxNode violation = variableDeclaration;
				Location location = violation.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location, identifier.ValueText);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private bool IsNameValid(Regex validator, SyntaxToken identifier)
		{
			var name = identifier.ValueText;

			return validator.IsMatch(name);
		}
	}
}
