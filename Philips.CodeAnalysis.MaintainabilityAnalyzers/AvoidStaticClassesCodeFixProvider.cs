// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidStaticClassesCodeFixProvider)), Shared]
	public class AvoidStaticClassesCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Whitelist this class";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AvoidStaticClasses)); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			TextDocument text = GetDocument(context.Document.Project, AvoidStaticClassesAnalyzer.FileName);

			if (text == null)
			{
				return;
			}

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			ClassDeclarationSyntax classDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

			context.RegisterCodeFix(
				CodeAction.Create(
					title: Title,
					createChangedSolution: c => AdditionalDocumentAppendLine(context.Document, text, classDeclaration, c),
					equivalenceKey: Title),
				diagnostic);
		}


		private static TextDocument GetDocument(Project project, string fileName)
		{
			return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(fileName, StringComparison.Ordinal));
		}


		private async Task<Solution> AdditionalDocumentAppendLine(Document document, TextDocument textDocument, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
		{
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			string newLine = semanticModel.GetDeclaredSymbol(classDeclaration).ToDisplayString();

			SourceText sourceText = await textDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
			SourceText newSourceText = AddLineToSourceText(sourceText, newLine);
			return textDocument.Project.Solution.WithAdditionalDocumentText(textDocument.Id, newSourceText);
		}

		private SourceText AddLineToSourceText(SourceText sourceText, string line)
		{
			List<string> list = new List<string>();
			foreach (TextLine textLine in sourceText.Lines)
			{
				list.Add(textLine.ToString());
			}
			list.Add(line);

			SourceText newSourceText = sourceText.Replace(0, sourceText.Length, string.Join(Environment.NewLine, list.ToArray()));
			return newSourceText;
		}
	}
}
