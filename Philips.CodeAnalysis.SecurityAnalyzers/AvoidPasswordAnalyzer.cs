// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
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

		public static readonly DiagnosticDescriptor Rule = new(
			Helper.ToDiagnosticId(DiagnosticId.AvoidPasswordField), 
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
			if (context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax)
			{
				var location = propertyDeclarationSyntax.GetLocation();
				Diagnose(propertyDeclarationSyntax.Identifier.ValueText, location, context.ReportDiagnostic);
			}
		}

		private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax)
			{
				var location = methodDeclarationSyntax.GetLocation();
				Diagnose(methodDeclarationSyntax.Identifier.ValueText,location, context.ReportDiagnostic);
			}
		}

		private void AnalyzeFields(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax)
			{
				foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
				{
					var location = fieldDeclarationSyntax.GetLocation();
					Diagnose(variable.Identifier.ValueText, location, context.ReportDiagnostic);
				}
			}
		}


		private void AnalyzeComments(SyntaxTreeAnalysisContext context)
		{
			SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
			var comments = root.DescendantTrivia().Where((t) => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));
			foreach (SyntaxTrivia comment in comments)
			{
				var location = comment.GetLocation();
				Diagnose(comment.ToString(), location, context.ReportDiagnostic);
			}
		}

		private Diagnostic CheckComment(string comment, Location location)
		{
#if NETCOREAPP
			if (comment.Contains(@"password", StringComparison.OrdinalIgnoreCase))
#else
			if (comment.ToLower(CultureInfo.CurrentCulture).Contains(@"password"))
#endif
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
