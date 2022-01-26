// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.SecurityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidPasswordAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid Password";
		public const string MessageFormat = @"Naming something Password suggests a potential hard-coded password.";
		private const string Description = @"Avoid hard-coded passwords.  (Avoid this analyzer by not naming something Password.)";
		private const string Category = Categories.Security;

		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			Helper.ToDiagnosticId(DiagnosticIds.AvoidPasswordField),
			Title, MessageFormat, Category,
			DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }


		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeFields, SyntaxKind.FieldDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
			context.RegisterSyntaxTreeAction(AnalyzeComments);
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			PropertyDeclarationSyntax propertyDeclarationSyntax = context.Node as PropertyDeclarationSyntax;
			if (propertyDeclarationSyntax != null)
				Diagnose(propertyDeclarationSyntax.Identifier.ValueText,
					propertyDeclarationSyntax.GetLocation(), context.ReportDiagnostic);
		}

		private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclarationSyntax = context.Node as MethodDeclarationSyntax;
			if (methodDeclarationSyntax != null)
				Diagnose(methodDeclarationSyntax.Identifier.ValueText,
					methodDeclarationSyntax.GetLocation(), context.ReportDiagnostic);
		}

		private void AnalyzeFields(SyntaxNodeAnalysisContext context)
		{
			FieldDeclarationSyntax fieldDeclarationSyntax = context.Node as FieldDeclarationSyntax;
			if (fieldDeclarationSyntax != null)
				foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
				{
					Diagnose(variable.Identifier.ValueText,
						fieldDeclarationSyntax.GetLocation(), context.ReportDiagnostic);
				}
		}


		private void AnalyzeComments(SyntaxTreeAnalysisContext context)
		{
			SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
			var comments = root.DescendantTrivia().Where((t) => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));
			foreach (SyntaxTrivia comment in comments)
			{
				Diagnose(comment.ToString(), comment.GetLocation(), context.ReportDiagnostic);
			}
		}

		private Diagnostic CheckComment(string comment, Location location)
		{
			if (comment.ToLower().Contains(@"password"))
			{
				return Diagnostic.Create(Rule, location);
			}
			return null;
		}

		private void Diagnose(string valueText, Location location, Action<Diagnostic> reportDiagnostic)
		{
			Diagnostic diagnostic = CheckComment(valueText, location);
			if (diagnostic != null)
			{
				reportDiagnostic(diagnostic);
			}
		}
	}
}
