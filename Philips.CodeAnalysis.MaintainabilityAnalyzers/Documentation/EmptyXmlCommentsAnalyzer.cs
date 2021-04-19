// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EmptyXmlCommentsAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Summary XML comments";
		private const string MessageFormat = @"Summary XML comments must be useful or non-existent.";
		private const string Description = @"Summary XML comments for classes, methods, etc. must be useful or non-existent.";
		private const string Category = Categories.Documentation;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.EmptyXmlComments), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.XmlElementStartTag);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			XmlElementStartTagSyntax elementStartTag = context.Node as XmlElementStartTagSyntax;

			if (elementStartTag.Parent.Parent.Kind() != SyntaxKind.SingleLineDocumentationCommentTrivia)
				return;

			if (elementStartTag.Name.LocalName.Text != @"summary")
				return;

			string content = (elementStartTag.Parent as XmlElementSyntax).Content.ToString();
			content = content.Replace('/', ' ');
			if (!string.IsNullOrWhiteSpace(content))
				return;

			Diagnostic diagnostic = Diagnostic.Create(Rule, elementStartTag.GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}
