using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestHasTimeoutAttributeAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"Test must have an appropriate Timeout";
		public const string MessageFormat = @"Test must have an appropriate Timeout attribute.";
		private const string Description = @"Tests that lack a Timeout may indefinitely block.";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.TestHasTimeoutAttribute),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, bool isDataTestMethod)
		{
			SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;

			if (!Helper.HasAttribute(attributeLists, context, MsTestFrameworkDefinitions.TimeoutAttribute, out Location timeoutLocation, out string argument))
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation());
				context.ReportDiagnostic(diagnostic);
				return;
			}

			if (Helper.HasAttribute(attributeLists, context, MsTestFrameworkDefinitions.TestCategoryAttribute, out Location categoryLocation, out string category)
				&& IsIncorrectTimeout(argument, category, out string errorText))
			{
				DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.TestHasTimeoutAttribute),
																   Title, errorText, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
				Diagnostic diagnostic = Diagnostic.Create(Rule, timeoutLocation);
				context.ReportDiagnostic(diagnostic);
			}
		}

		public const string UnitTestTimeoutIncorrect = "TestTimeout for Category UnitTests should be either CiAppropriate or CiAcceptable";
		public const string IntegrationTimeoutIncorrect = "TestTimeout for Category Integration should be Integration";
		public const string SmokeTimeoutIncorrect = "TestTimeout for Category Smoke should be Smoke";

		public static bool IsIncorrectTimeout(string argument, string category, out string messageFormat)
		{
			if (category.Equals("TestDefinitions.UnitTests") && (!argument.Equals("TestTimeouts.CiAppropriate") && !argument.Equals("TestTimeouts.CiAcceptable")))
			{
				messageFormat = UnitTestTimeoutIncorrect;
				return true;
			}
			else if (category.Equals("TestDefinitions.IntegrationTests") && !argument.Equals("TestTimeouts.Integration"))
			{
				messageFormat = IntegrationTimeoutIncorrect;
				return true;
			}
			else if (category.Equals("TestDefinitions.SmokeTests") && !argument.Equals("TestTimeouts.Smoke"))
			{
				messageFormat = SmokeTimeoutIncorrect;
				return true;
			}

			messageFormat = string.Empty;
			return false;
		}
	}
}

