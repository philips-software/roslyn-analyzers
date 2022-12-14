// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestMethodNameAnalyzer : DiagnosticAnalyzer
	{
		public const string MessageFormat = @"Test Method must not start with '{0}'";
		private const string Title = @"Test Method names unhelpful prefix'";
		private const string Description = @"Test Method names must not start with 'Test', 'Ensure', or 'Verify'. Otherwise, they are more difficult to find in sorted lists in Test Explorer.";
		private const string Category = Categories.Naming;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.TestMethodName), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.AttributeList);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			AttributeListSyntax attributesNode = (AttributeListSyntax)context.Node;

			// Only interested in TestMethod attributes
			bool found = false;
			foreach (AttributeSyntax attribute in attributesNode.Attributes)
			{
				if (attribute.Name.ToString() == @"TestMethod")
					found = true;
			}
			if (!found) return;

			SyntaxNode methodNode = attributesNode.Parent;

			// Confirm this is actually a method...
			if (methodNode.Kind() != SyntaxKind.MethodDeclaration)
				return;

			string invalidPrefix = string.Empty;
			foreach (SyntaxToken token in methodNode.ChildTokens())
			{
				if (token.Kind() == SyntaxKind.IdentifierToken)
				{
					// It's not mandatory to end with Test
					//if (!token.ValueText.EndsWith(@"Test"))
					//{
					//	Diagnostic diagnostic = Diagnostic.Create(Rule, token.GetLocation(), @"end");
					//	context.ReportDiagnostic(diagnostic);
					//	return;
					//}
					if (token.ValueText.StartsWith(@"Test"))
					{
						invalidPrefix = @"Test";
					}
					else if (token.ValueText.StartsWith(@"Ensure"))
					{
						invalidPrefix = @"Ensure";
					}
					else if (token.ValueText.StartsWith(@"Verify"))
					{
						invalidPrefix = @"Verify";
					}

					if (!string.IsNullOrEmpty(invalidPrefix))
					{
						Diagnostic diagnostic = Diagnostic.Create(Rule, token.GetLocation(), invalidPrefix);
						context.ReportDiagnostic(diagnostic);
						return;
					}
				}
			}
		}
	}
}
