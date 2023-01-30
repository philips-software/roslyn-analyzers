﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class EnforceBoolNamingConventionAnalyzer : DiagnosticAnalyzer
	{
		private static readonly Regex _privateFieldRegex = new(@"^(_(is|are|should|has|does|was))[A-Z0-9].*$", RegexOptions.Singleline);
		private static readonly Regex _publicFieldRegex = new(@"^(Is|Are|Should|Has|Does|Was)[A-Z0-9].*$", RegexOptions.Singleline);
		private static readonly Regex _localRegex = new(@"^(is|are|should|has|does|was)[A-Z0-9].*$", RegexOptions.Singleline);

		private const string Title = @"Follow variable naming coding guidelines";
		private const string MessageFormat = @"Rename variable '{0}' to fit coding guidelines";
		private const string Description = @"";
		private const string Category = Categories.Naming;

		private readonly bool _checkLocalVariables;
		private readonly bool _checkFieldVariables;

		public EnforceBoolNamingConventionAnalyzer() : this(true, true) { }

		public EnforceBoolNamingConventionAnalyzer(bool checkLocalVariables, bool checkFieldVariables)
		{
			_checkLocalVariables = checkLocalVariables;
			_checkFieldVariables = checkFieldVariables;
		}

		public static readonly DiagnosticDescriptor Rule = 
			new(Helper.ToDiagnosticId(DiagnosticIds.EnforceBoolNamingConvention), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
			context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}

		private void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			ForEachStatementSyntax foreachStatement = (ForEachStatementSyntax)context.Node;
			if (!IsTypeBool(foreachStatement.Type, context.SemanticModel))
			{
				return;
			}

			Regex validator = _localRegex;

			if (IsNameValid(validator, foreachStatement.Identifier))
			{
				return;
			}

			var location = foreachStatement.Identifier.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rule, location, foreachStatement.Identifier.ValueText);
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

			foreach (VariableDeclaratorSyntax syntax in variableDeclaration.Variables)
			{
				if (!ShouldCheck(variableDeclaration, context, out Regex validator))
				{
					continue;
				}

				if (IsNameValid(validator, syntax.Identifier))
				{
					continue;
				}

				var location = syntax.Identifier.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(Rule, location, syntax.Identifier.ValueText);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private bool ShouldCheck(VariableDeclarationSyntax variableDeclaration, SyntaxNodeAnalysisContext context, out Regex validator)
		{
			validator = null;
			bool shouldCheck;
			switch (variableDeclaration.Parent.Kind())
			{
				case SyntaxKind.LocalDeclarationStatement:
					{
						LocalDeclarationStatementSyntax localDeclaration = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;
						if (!IsTypeBool(localDeclaration.Declaration.Type, context.SemanticModel))
						{
							return false;
						}

						shouldCheck = _checkLocalVariables;
						validator = _localRegex;
						break;
					}
				case SyntaxKind.FieldDeclaration:
					{
						FieldDeclarationSyntax fieldDeclaration = (FieldDeclarationSyntax)variableDeclaration.Parent;

						if (!IsTypeBool(fieldDeclaration.Declaration.Type, context.SemanticModel))
						{
							return false;
						}

						shouldCheck = _checkFieldVariables;

						if (IsFieldPublic(fieldDeclaration) || IsFieldConst(fieldDeclaration))
						{
							validator = _publicFieldRegex;
						}
						else
						{
							validator = _privateFieldRegex;
						}

						break;
					}
				default:
					shouldCheck = false;
					validator = _privateFieldRegex;
					break;
			}
			return shouldCheck;
		}

		private void AnalyzeParameter(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			ParameterSyntax parameter = (ParameterSyntax)context.Node;
			if (!IsTypeBool(parameter.Type, context.SemanticModel))
			{
				return;
			}

			IMethodSymbol method = (IMethodSymbol)context.ContainingSymbol;
			if (method.IsOverride)
			{
				return;
			}

			Regex validator = _localRegex;

			if (IsNameValid(validator, parameter.Identifier))
			{
				return;
			}

			var location = parameter.Identifier.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rule, location, parameter.Identifier.ValueText);
			context.ReportDiagnostic(diagnostic);
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			PropertyDeclarationSyntax property = (PropertyDeclarationSyntax)context.Node;
			if (!IsTypeBool(property.Type, context.SemanticModel))
			{
				return;
			}

			if (property.Modifiers.Any(SyntaxKind.OverrideKeyword))
			{
				return;
			}

			var type = context.SemanticModel.GetDeclaredSymbol(property);
			if (type is IPropertySymbol propertySymbol && propertySymbol.ContainingType.AllInterfaces.Any(x => x.GetMembers(propertySymbol.Name).Any()))
			{
				return;
			}

			Regex validator = _publicFieldRegex;

			if (IsNameValid(validator, property.Identifier))
			{
				return;
			}

			var location = property.Identifier.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rule, location, property.Identifier.ValueText);
			context.ReportDiagnostic(diagnostic);
		}

		private bool IsNameValid(Regex validator, SyntaxToken identifier)
		{
			string name = identifier.ValueText;

			return validator.IsMatch(name);
		}

		private bool IsTypeBool(TypeSyntax typeSyntax, SemanticModel semanticModel)
		{
			if (typeSyntax != null)
			{
				TypeInfo type = semanticModel.GetTypeInfo(typeSyntax);
				if (type.Type != null)
				{
					return type.Type.SpecialType == SpecialType.System_Boolean;
				}
			}

			return false;
		}

		private bool IsFieldPublic(FieldDeclarationSyntax fieldDeclaration)
		{
			return fieldDeclaration.Modifiers.Any(x => x.Kind() == SyntaxKind.PublicKeyword);
		}

		private bool IsFieldConst(FieldDeclarationSyntax fieldDeclaration)
		{
			return fieldDeclaration.Modifiers.Any(x => x.Kind() == SyntaxKind.ConstKeyword);
		}
	}
}
