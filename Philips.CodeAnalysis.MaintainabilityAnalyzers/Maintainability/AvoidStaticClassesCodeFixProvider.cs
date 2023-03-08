// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidStaticClassesCodeFixProvider)), Shared]
	public class AvoidStaticClassesCodeFixProvider : SolutionCodeFixProvider<ClassDeclarationSyntax>
	{
		protected override string Title => "Whitelist this class";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidStaticClasses;

		protected override string GetAdditionalFileName()
		{
			return AvoidStaticClassesAnalyzer.AllowedFileName;
		}

		protected override async Task<Solution> ApplyFix(Document document, ClassDeclarationSyntax node, CancellationToken cancellationToken)
		{
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			var newLine = semanticModel.GetDeclaredSymbol(node, cancellationToken).ToDisplayString();
			TextDocument textDocument = AdditionalFileDocument;

			SourceText sourceText = await textDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
			SourceText newSourceText = AddLineToSourceText(sourceText, newLine);
			return textDocument.Project.Solution.WithAdditionalDocumentText(textDocument.Id, newSourceText);
		}

		private SourceText AddLineToSourceText(SourceText sourceText, string line)
		{
			List<string> list = new();
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
