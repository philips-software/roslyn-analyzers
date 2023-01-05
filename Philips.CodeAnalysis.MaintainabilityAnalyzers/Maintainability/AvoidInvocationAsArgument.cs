// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidInvocationAsArgumentAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid method calls as arguments";
		public const string MessageFormat = @"Avoid '{0}' as an argument";
		private const string Description = @"Avoid method calls as arguments to method calls";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidInvocationAsArgument), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Argument);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			ArgumentSyntax argumentSyntax = (ArgumentSyntax)context.Node;

			// We are looking for method calls as arguments
			if (argumentSyntax.Expression is not InvocationExpressionSyntax argumentExpressionSyntax)
			{
				return;
			}

			// If it's an embedded nameof() operation, let it go.
			if ((argumentExpressionSyntax.Expression as IdentifierNameSyntax)?.Identifier.Text == "nameof")
			{
				return;
			}

			// If it's calling ToString(), let it go. (ToStrings() cognitive load isn't excessive, and lots of violations)
			if ((argumentExpressionSyntax.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text == "ToString")
			{
				return;
			}

			// If nested calls (e.g., Foo(Bar(Meow()))), only trigger the outer violation Bar(Meow())
			if (argumentSyntax.Ancestors().OfType<ArgumentSyntax>().Any())
			{
				return;
			}

			// If we're within a constructor initializer (this(...) or base(...) eg), let it go
			ConstructorInitializerSyntax constructorInitializerSyntax = argumentSyntax.Ancestors().OfType<ConstructorInitializerSyntax>().FirstOrDefault();
			if (constructorInitializerSyntax != null)
			{
				return;
			}

			// If the caller is Assert, let it go. (This is debatable, and ideally warrants a configuration option.)
			MemberAccessExpressionSyntax caller = (argumentSyntax.Parent.Parent as InvocationExpressionSyntax)?.Expression as MemberAccessExpressionSyntax;
			if (caller?.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText.Contains(@"Assert"))
			{
				return;
			}

			// If the called method is static, let it go to reduce annoyances. E.g., "Times.Once", "Mock.Of<>", "Marshal.Sizeof", etc.
			// This is debatable, and ideally warrants a configuration option.
			if (argumentExpressionSyntax.Expression is MemberAccessExpressionSyntax callee)
			{
				var symbol = context.SemanticModel.GetSymbolInfo(callee).Symbol;
				if (symbol.IsStatic)
				{
					return;
				}
			}

			Diagnostic diagnostic = Diagnostic.Create(Rule, argumentSyntax.GetLocation(), argumentSyntax.ToString());
			context.ReportDiagnostic(diagnostic);
		}
	}
}
