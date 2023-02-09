// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.SecurityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class RegexNeedsTimeoutAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"RegEx needs a timeout";
		public const string MessageFormat = @"When constructing a new Regex instance, provide a timeout.";
		private const string Description = @"When constructing a new Regex instance, provide a timeout (or `RegexOptions.NonBacktracking` in .NET 7 and higher) as this can facilitate denial-of-serice attacks.";
		private const string Category = Categories.Security;
		private const int CorrectConstructorArgumentCount = 3;
		private readonly TestHelper _helper = new();

		public static readonly DiagnosticDescriptor Rule = new(
			Helper.ToDiagnosticId(DiagnosticId.RegexNeedsTimeout),
			Title, MessageFormat, Category,
			DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var creation = (ObjectCreationExpressionSyntax)context.Node;

			if (_helper.IsInTestClass(context))
			{
				return;
			}

			// Bail out early.
			TypeSyntax typeSyntax = creation.Type;
			if (!typeSyntax.ToString().Contains("Regex"))
			{
				return;
			}

			var typeSymbol = context.SemanticModel.GetTypeInfo(creation).Type;
			if (typeSymbol == null)
			{
				return;
			}

			// Double check if the is a Regex constructor.
			if (typeSymbol.ToString() != "System.Text.RegularExpressions.Regex")
			{
				return;
			}

			// We require to use the constructor with the Timeout argument.
			if (creation.ArgumentList is not { Arguments.Count: not CorrectConstructorArgumentCount })
			{
				return;
			}

			// NET7 has RegexOptions.NonBacktracking, which we also accept.
			if (creation.ArgumentList.ToString().Contains("NonBacktracking"))
			{
				return;
			}

			var location = creation.ArgumentList.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rule, location);
			context.ReportDiagnostic(diagnostic);
		}
	}
}
