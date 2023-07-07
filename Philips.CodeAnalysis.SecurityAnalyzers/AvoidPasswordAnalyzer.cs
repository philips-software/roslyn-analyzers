// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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
	public class AvoidPasswordAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid Password";
		public const string MessageFormat = @"Naming something Password suggests a potential hard-coded password.";
		private const string Description = @"Avoid hard-coded passwords.  (Avoid this analyzer by not naming something Password.)";

		private const string MsTestMetadataReference = "Microsoft.VisualStudio.TestTools.UnitTesting.Assert";

		public virtual bool ShouldAnalyzeTests { get; set; }

		public AvoidPasswordAnalyzer()
			: base(DiagnosticId.AvoidPasswordField, Title, MessageFormat, Description, Categories.Security)
		{ }

		protected override void InitializeAnalysis(CompilationStartAnalysisContext context)
		{
			if (ShouldAnalyzeTests || context.Compilation.GetTypeByMetadataName(MsTestMetadataReference) == null)
			{
				context.RegisterSyntaxNodeAction(AnalyzeFields, SyntaxKind.FieldDeclaration);
				context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
				context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
				context.RegisterSyntaxTreeAction(AnalyzeComments);
			}
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax)
			{
				Location location = propertyDeclarationSyntax.GetLocation();
				Diagnose(propertyDeclarationSyntax.Identifier.ValueText, location, context.ReportDiagnostic);
			}
		}

		private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax)
			{
				Location location = methodDeclarationSyntax.GetLocation();
				Diagnose(methodDeclarationSyntax.Identifier.ValueText, location, context.ReportDiagnostic);
			}
		}

		private void AnalyzeFields(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax)
			{
				foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
				{
					Location location = fieldDeclarationSyntax.GetLocation();
					Diagnose(variable.Identifier.ValueText, location, context.ReportDiagnostic);
				}
			}
		}


		private void AnalyzeComments(SyntaxTreeAnalysisContext context)
		{
			SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
			System.Collections.Generic.IEnumerable<SyntaxTrivia> comments = root.DescendantTrivia().Where((t) => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));
			foreach (SyntaxTrivia comment in comments)
			{
				Location location = comment.GetLocation();
				Diagnose(comment.ToString(), location, context.ReportDiagnostic);
			}
		}

		private Diagnostic Check(string comment, Location location)
		{
			if (comment.ToLower(CultureInfo.CurrentCulture).Contains(@"password"))
			{
				return Diagnostic.Create(Rule, location);
			}
			return null;
		}

		private void Diagnose(string valueText, Location location, Action<Diagnostic> reportDiagnostic)
		{
			Diagnostic diagnostic = Check(valueText, location);
			if (diagnostic != null)
			{
				reportDiagnostic(diagnostic);
			}
		}
	}
}
