// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
	public class VariableNamingConventionAnalyzer : DiagnosticAnalyzer
	{
		private static readonly Regex _fieldRegex = new(@"^(_|[A-Z]).*$", RegexOptions.Singleline);
		private static readonly Regex _localRegex = new(@"^([a-z]|[A-Z]).*$", RegexOptions.Singleline);
		private static readonly Regex _eventRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Singleline);

		private const string Title = @"Follow variable naming coding guidelines";
		private const string MessageFormat = @"Rename variable '{0}' to fit coding guidelines";
		private const string Description = @"";
		private const string Category = Categories.Naming;

		private readonly bool _checkLocalVariables;
		private readonly bool _checkFieldVariables;

		public VariableNamingConventionAnalyzer() : this(true, true) { }

		public VariableNamingConventionAnalyzer(bool checkLocalVariables, bool checkFieldVariables)
		{
			_checkLocalVariables = checkLocalVariables;
			_checkFieldVariables = checkFieldVariables;
		}

		public List<DiagnosticDescriptor> Rules { get; } = new()
		{
			new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.VariableNamingConventions), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description),
		};

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rules.ToArray()); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
		}

		private void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			ForEachStatementSyntax foreachStatement = (ForEachStatementSyntax)context.Node;

			Regex validator = _localRegex;

			if (IsNameValid(validator, foreachStatement.Identifier))
			{
				return;
			}

			CSharpSyntaxNode violation = foreachStatement;
			Diagnostic diagnostic = Diagnostic.Create(Rules[0], violation.GetLocation(), foreachStatement.Identifier.ValueText);
			context.ReportDiagnostic(diagnostic);
		}

		private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			VariableDeclarationSyntax variableDeclaration = (VariableDeclarationSyntax)context.Node;

			foreach (var syntax in variableDeclaration.Variables)
			{
				bool shouldCheck;
				Regex validator;
				switch (variableDeclaration.Parent.Kind())
				{
					case SyntaxKind.ForStatement:
					case SyntaxKind.UsingStatement:
						shouldCheck = _checkLocalVariables;
						validator = _localRegex;
						break;
					case SyntaxKind.LocalDeclarationStatement:
						{
							shouldCheck = _checkLocalVariables;
							validator = _localRegex;
							break;
						}
					case SyntaxKind.FieldDeclaration:
						{
							FieldDeclarationSyntax fieldDeclaration = (FieldDeclarationSyntax)variableDeclaration.Parent;

							if (fieldDeclaration.Modifiers.Any(x => x.Kind() == SyntaxKind.PublicKeyword))
							{
								continue;
							}

							shouldCheck = _checkFieldVariables;

							validator = _fieldRegex;
							break;
						}
					case SyntaxKind.EventFieldDeclaration:
						shouldCheck = _checkFieldVariables;
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

				if (IsNameValid(validator, syntax.Identifier))
				{
					continue;
				}

				CSharpSyntaxNode violation = variableDeclaration;
				Diagnostic diagnostic = Diagnostic.Create(Rules[0], violation.GetLocation(), syntax.Identifier.ValueText);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private bool IsNameValid(Regex validator, SyntaxToken identifier)
		{
			string name = identifier.ValueText;

			if (validator.IsMatch(name))
			{
				return true;
			}

			return false;
		}
	}
}
