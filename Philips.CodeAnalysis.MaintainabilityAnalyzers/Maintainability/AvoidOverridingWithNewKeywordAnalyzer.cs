// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Diagnostic of overriding methods or properties with the new keyword.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidOverridingWithNewKeywordAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = "Avoid overriding methods or properties with the new keyword.";
		private const string MessageFormat = "Avoid overriding {0} with the new keyword.";
		private const string Description = "Overriding with the new keyword gives unexpected behavior for the callers of the overridden method or property.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule =
			new(
				Helper.ToDiagnosticId(DiagnosticIds.AvoidOverridingWithNewKeyword),
				Title,
				MessageFormat,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: Description
			);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if(generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			var property = (PropertyDeclarationSyntax)context.Node;

			if (property.Modifiers.Any(SyntaxKind.NewKeyword))
			{
				var location = property.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, property.Identifier.Text));
			}
		}
		private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			var method = (MethodDeclarationSyntax)context.Node;
			
			if (method.Modifiers.Any(SyntaxKind.NewKeyword))
			{
				var location = method.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, method.Identifier.Text));
			}
		}
	}
}
