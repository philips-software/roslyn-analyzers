// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class TestShouldUseDisplayNameAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"Test should use DisplayName or Description attribute instead of comments";
		public const string MessageFormat = @"Consider using DisplayName parameter for DataRow or Description attribute for test method instead of inline comments";
		private const string Description = @"Using DisplayName parameter for DataRow attributes or Description attribute for test methods makes test purpose more visible in test runners and provides better documentation.";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.UseDisplayNameOrDescription.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return new TestShouldUseDisplayName(definitions, Helper);
		}

		public class TestShouldUseDisplayName : TestMethodImplementation
		{
			public TestShouldUseDisplayName(MsTestAttributeDefinitions definitions, Helper helper) : base(definitions, helper)
			{ }

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				if (isDataTestMethod)
				{
					CheckDataTestMethodForDisplayName(context, methodDeclaration);
				}
				else
				{
					CheckTestMethodForDescription(context, methodDeclaration);
				}
			}

			private void CheckDataTestMethodForDisplayName(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
			{
				// Check for DataRow attributes with comments but no DisplayName
				foreach (AttributeListSyntax attributeList in methodDeclaration.AttributeLists)
				{
					foreach (AttributeSyntax attribute in attributeList.Attributes)
					{
						if (Helper.ForAttributes.IsDataRowAttribute(attribute, context))
						{
							// Check if this DataRow has a comment but no DisplayName
							var hasDisplayName = attribute.ArgumentList?.Arguments.Any(arg =>
								arg.NameEquals?.Name.Identifier.ValueText == "DisplayName") == true;

							if (!hasDisplayName)
							{
								// Look for trailing comment on the same line
								var comment = GetTrailingComment(attribute);
								if (!string.IsNullOrWhiteSpace(comment))
								{
									var diagnostic = Diagnostic.Create(Rule, attribute.GetLocation());
									context.ReportDiagnostic(diagnostic);
								}
							}
						}
					}
				}
			}

			private void CheckTestMethodForDescription(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
			{
				// Check if test method has Description attribute
				var hasDescription = Helper.ForAttributes.HasAttribute(methodDeclaration.AttributeLists, context, MsTestFrameworkDefinitions.DescriptionAttribute, out _, out _);

				if (!hasDescription)
				{
					// Look for leading comment before the method
					var comment = GetLeadingComment(methodDeclaration);
					if (!string.IsNullOrWhiteSpace(comment))
					{
						var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation());
						context.ReportDiagnostic(diagnostic);
					}
				}
			}

			private string GetTrailingComment(AttributeSyntax attribute)
			{
				SyntaxToken token = attribute.GetLastToken();
				SyntaxTrivia trivia = token.TrailingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));

				if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
				{
					return ExtractCommentText(trivia.ToString());
				}

				return string.Empty;
			}

			private string GetLeadingComment(MethodDeclarationSyntax methodDeclaration)
			{
				SyntaxTriviaList leadingTrivia = methodDeclaration.GetLeadingTrivia();

				// Look for single-line comment immediately before the method
				SyntaxTrivia comment = leadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));
				if (comment.IsKind(SyntaxKind.SingleLineCommentTrivia))
				{
					return ExtractCommentText(comment.ToString());
				}

				return string.Empty;
			}

			private string ExtractCommentText(string commentTrivia)
			{
				if (string.IsNullOrWhiteSpace(commentTrivia))
				{
					return string.Empty;
				}

				// Remove // and trim whitespace
				var text = commentTrivia.Trim();
				if (text.StartsWith("//"))
				{
					text = text.Substring(2).Trim();
				}

				// Only consider meaningful comments (more than just a few characters)
				if (text.Length > 5)
				{
					return text;
				}

				return string.Empty;
			}
		}
	}
}
