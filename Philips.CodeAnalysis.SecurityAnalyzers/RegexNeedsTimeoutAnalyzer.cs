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
	public class RegexNeedsTimeoutAnalyzer : DiagnosticAnalyzerBase
	{
		private const string Title = @"Regex needs a timeout";
		public const string MessageFormat = @"When constructing a new Regex instance, provide a timeout.";
		private const string Description = @"When constructing a new Regex instance, provide a timeout (or `RegexOptions.NonBacktracking` in .NET 7 and higher) as this can facilitate denial-of-serice attacks.";
		private const string Category = Categories.Security;
		private const int CorrectConstructorArgumentCount = 3;

		public static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId.RegexNeedsTimeout.ToId(),
			Title, MessageFormat, Category,
			DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var creation = (ObjectCreationExpressionSyntax)context.Node;

			if (Helper.ForTests.IsInTestClass(context))
			{
				return;
			}

			TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(creation);
			ITypeSymbol typeSymbol = typeInfo.Type;
			var usedConvertedType = false;

			// For implicit constructors like new (".*"), Type might be a tuple (?, ?) but ConvertedType has the actual type
			// Only use ConvertedType if Type is null or appears to be an incomplete/invalid type
			if (typeSymbol == null || typeSymbol.ToString().Contains("?"))
			{
				typeSymbol = typeInfo.ConvertedType;
				usedConvertedType = true;
			}

			if (typeSymbol == null)
			{
				return;
			}

			// Double check if the is a Regex constructor.
			if (typeSymbol.ToString() != "System.Text.RegularExpressions.Regex")
			{
				return;
			}

			// Skip incomplete implicit constructor nodes that have no arguments but are resolved via ConvertedType
			// These appear to be parser artifacts and the real analysis happens on the complete nodes
			if (usedConvertedType && creation.ArgumentList?.Arguments.Count == 0)
			{
				return;
			}

			AnalyzeCreation(context, creation.ArgumentList);
		}

		private void AnalyzeCreation(SyntaxNodeAnalysisContext context, ArgumentListSyntax argumentList)
		{
			// We require to use the constructor with the Timeout argument.
			if (argumentList is not { Arguments.Count: not CorrectConstructorArgumentCount })
			{
				return;
			}

			// NET7 has RegexOptions.NonBacktracking, which we also accept.
			if (argumentList.ToString().Contains("NonBacktracking"))
			{
				return;
			}

			Location location = argumentList.GetLocation();
			var diagnostic = Diagnostic.Create(Rule, location);
			context.ReportDiagnostic(diagnostic);
		}
	}
}