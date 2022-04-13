// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	/// <summary>
	/// Access modifier must be explicitly stated, instead of relying on the default value.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnforceAccessModifierAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = "Access modifier must be explicitly stated";
		private const string Message =
			"Missing access modifiers in '{0}'. Access modifier must be explicitly stated, instead of relying on the default value.";
		private const string Description = "Access modifier must be explicitly stated";
		private const string Category = Categories.Readability;

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.EnforceAccessModifier),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: false,
				description: Description);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(
				AnalyzeClass,
				SyntaxKind.ClassDeclaration);
			context.RegisterSyntaxNodeAction(
				AnalyzeField,
				SyntaxKind.FieldDeclaration);
			context.RegisterSyntaxNodeAction(
				AnalyzeMethod,
				SyntaxKind.MethodDeclaration);
			context.RegisterSyntaxNodeAction(
				AnalyzeProperty,
				SyntaxKind.PropertyDeclaration);
		}

		private void AnalyzeClass(SyntaxNodeAnalysisContext context)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			if (!classDeclaration.Modifiers.Where(ModifiersFilter).Any())
			{
				ReportDiagnostics(context, classDeclaration, classDeclaration.Identifier.Text);
			}
		}

		private void AnalyzeField(SyntaxNodeAnalysisContext context)
		{
			var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
			if (!fieldDeclaration.Modifiers.Where(ModifiersFilter).Any())
			{
				var fieldName = fieldDeclaration.Declaration.Variables.First().Identifier.Text;
				ReportDiagnostics(context, fieldDeclaration, fieldName);
			}
		}

		private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			var methodDeclaration = (MethodDeclarationSyntax)context.Node;
			if (!methodDeclaration.Modifiers.Where(ModifiersFilter).Any())
			{
				ReportDiagnostics(context, methodDeclaration, methodDeclaration.Identifier.Text);
			}
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
			if (!propertyDeclaration.Modifiers.Where(ModifiersFilter).Any())
			{
				ReportDiagnostics(context, propertyDeclaration, propertyDeclaration.Identifier.Text);
			}
		}

		private void ReportDiagnostics(SyntaxNodeAnalysisContext context, SyntaxNode node, string name)
		{
			var newLineLocation = node.GetLocation();
			context.ReportDiagnostic(Diagnostic.Create(Rule, newLineLocation, name));
		}

		private bool ModifiersFilter(SyntaxToken token)
		{
			return token.IsKind(SyntaxKind.PublicKeyword) ||
			       token.IsKind(SyntaxKind.PrivateKeyword) ||
				   token.IsKind(SyntaxKind.InternalKeyword) ||
			       token.IsKind(SyntaxKind.ProtectedKeyword);
		}
	}
}
