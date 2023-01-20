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
	/// <summary>
	/// Note that a CodeFixer isn't necessary. At the time of this writing, VS offers a refactoring but seemingly not an analyzer.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidUnnecessaryWhereAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid unnecessary 'Where'";
		public const string MessageFormat = @"Move predicate from 'Where' to '{0}'";
		private const string Description = @"Invoking Where is unnecessary";
		private const string HelpUri = @"https://learn.microsoft.com/en-us/visualstudio/ide/reference/simplify-linq-expression?view=vs-2022";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidUnnecessaryWhere), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description, helpLinkUri: HelpUri);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			InvocationExpressionSyntax invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;

			// Is this a call to Count(), Any(), etc, w/o a predicate?
			if (invocationExpressionSyntax.ArgumentList.Arguments.Count != 0)
			{
				return;
			}
			if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax expressionOfInterest)
			{
				return;
			}
			if (expressionOfInterest.Name.Identifier.Text is not @"Count"
					and not @"Any"
					and not @"Single"
					and not @"SingleOrDefault"
					and not @"Last"
					and not @"LastOrDefault"
					and not @"First"
					and not @"FirstOrDefault"
			)
			{
				return;
			}

			// Is it from a Where clause?
			if (expressionOfInterest.Expression is not InvocationExpressionSyntax whereInvocationExpression)
			{
				return;
			}
			if (whereInvocationExpression.Expression is not MemberAccessExpressionSyntax whereExpression)
			{
				return;
			}
			if (whereExpression.Name.Identifier.Text is not @"Where")
			{
				return;
			}

			// It's practicially guaranteed we found something, but let's confirm it's System.Linq.Where
			var whereSymbol = context.SemanticModel.GetSymbolInfo(whereExpression.Name).Symbol as IMethodSymbol;
			string strWhereSymbol = whereSymbol?.ToString();
			if (strWhereSymbol != null && strWhereSymbol.StartsWith(@"System.Collections.Generic.IEnumerable"))
			{ 
				var location = whereExpression.Name.Identifier.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(Rule, location, expressionOfInterest.Name.Identifier.Text);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
