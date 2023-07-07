// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ProhibitDynamicKeywordAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Prohibit the ""dynamic"" Keyword";
		private const string MessageFormat = @"Do not use the ""dynamic"" keyword.  It it not compile time type safe.";
		private const string Description = @"The ""dynamic"" keyword is not checked for type safety at compile time.";
		private const string DynamicIdentifier = "dynamic";

		public ProhibitDynamicKeywordAnalyzer()
			: base(DiagnosticId.DynamicKeywordProhibited, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		protected override void InitializeAnalysis(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeVariable, SyntaxKind.VariableDeclaration);
		}

		private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			var method = (MethodDeclarationSyntax)context.Node;
			if (IsDynamicType(context, method.ReturnType))
			{
				Location returnLocation = method.ReturnType.GetLocation();
				ReportDiagnostic(context, returnLocation);
			}
			if (HasDynamicType(context, method.ParameterList))
			{
				Location location = method.ParameterList.GetLocation();
				ReportDiagnostic(context, location);
			}
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			var prop = (PropertyDeclarationSyntax)context.Node;
			if (IsDynamicType(context, prop.Type))
			{
				Location returnLocation = prop.Type.GetLocation();
				ReportDiagnostic(context, returnLocation);
			}
		}

		private void AnalyzeVariable(SyntaxNodeAnalysisContext context)
		{
			var variable = (VariableDeclarationSyntax)context.Node;
			if (IsDynamicType(context, variable.Type))
			{
				Location returnLocation = variable.Type.GetLocation();
				ReportDiagnostic(context, returnLocation);
			}
			if (variable.Variables.Any(v => HasDynamicType(context, v.Initializer)))
			{
				Location returnLocation = variable.Type.GetLocation();
				ReportDiagnostic(context, returnLocation);
			}
		}

		private static bool IsDynamicType(SyntaxNodeAnalysisContext context, TypeSyntax typeSyntax)
		{
			if (typeSyntax is SimpleNameSyntax { Identifier.ValueText: DynamicIdentifier })
			{
				// Double check the semantic model (to distinguish a variable named 'dynamic').
				SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(typeSyntax);
				if (symbol.Symbol is IDynamicTypeSymbol)
				{
					return true;
				}
			}

			if (typeSyntax is GenericNameSyntax generic && generic.TypeArgumentList.Arguments.Any(gen => IsDynamicType(context, gen)))
			{
				return true;
			}

			return false;
		}

		private static bool HasDynamicType(SyntaxNodeAnalysisContext context, SyntaxNode node)
		{
			return node != null && node.DescendantNodes().OfType<SimpleNameSyntax>().Any(id => IsDynamicType(context, id));
		}

		private void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location)
		{
			context.ReportDiagnostic(Diagnostic.Create(Rule, location));
		}
	}
}
