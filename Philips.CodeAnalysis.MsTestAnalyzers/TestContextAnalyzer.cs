// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestContextAnalyzer : DiagnosticAnalyzer
	{
		public const string MessageFormat = @"TestContext should be used or removed.";
		private const string Title = @"TestContext Usage";
		private const string Description = @"TestContext should not be included in test classes unless it is actually used.";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.TestContext.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestContext") == null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.PropertyDeclaration);
			});
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var property = (PropertyDeclarationSyntax)context.Node;
			if (property.Type.ToString() != @"TestContext")
			{
				return;
			}

			if ((context.SemanticModel.GetSymbolInfo(property.Type).Symbol is not ITypeSymbol symbol) || (symbol.ToString() != @"Microsoft.VisualStudio.TestTools.UnitTesting.TestContext"))
			{
				return;
			}

			var varName = string.Empty;
			var propName = property.Identifier.ToString();
			IEnumerable<SyntaxNode> propNodes = context.Node.DescendantNodes();
			IEnumerable<ReturnStatementSyntax> returnNodes = propNodes.OfType<ReturnStatementSyntax>();
			if (returnNodes.Any())
			{
				ReturnStatementSyntax returnStatement = returnNodes.First();
				if (returnStatement?.Expression is IdentifierNameSyntax returnVar)
				{
					varName = returnVar.Identifier.ToString();
				}
			}

			// find out if the property or its underlying variable is actually used
			foreach (IdentifierNameSyntax identifier in context.Node.Parent.DescendantNodes().OfType<IdentifierNameSyntax>())
			{
				if ((identifier.Identifier.ToString() == propName) &&
					(identifier.Parent != property) &&
					context.SemanticModel.GetSymbolInfo(identifier).Symbol is not ITypeSymbol)
				{
					// if we find the same identifier as the propery and it's not a type or the original instance, it's used
					return;
				}

				if ((identifier.Identifier.ToString() == varName) &&
					identifier.Parent is not VariableDeclarationSyntax &&
					!propNodes.Contains(identifier) &&
					context.SemanticModel.GetSymbolInfo(identifier).Symbol is not ITypeSymbol)
				{
					// if we find the same identifier as the variable and it's not a type, the original declaration, or part of the property, it's used
					return;
				}
			}

			// if not, report a diagnostic error
			Location location = context.Node.GetLocation();
			var diagnostic = Diagnostic.Create(Rule, location);
			context.ReportDiagnostic(diagnostic);
			return;
		}
	}
}
