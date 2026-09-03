// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidUnderscoresInTestMethodNameCodeFixProvider)), Shared]
	public class AvoidUnderscoresInTestMethodNameCodeFixProvider : SolutionCodeFixProvider<MethodDeclarationSyntax>
	{
		private static readonly char[] UnderscoreSeparator = ['_'];

		protected override string Title => "Remove underscores from test method name";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidUnderscoresInTestMethodName;

		protected override async Task<Solution> ApplyFix(Document document, MethodDeclarationSyntax node, CancellationToken cancellationToken)
		{
			var name = node.Identifier.Text;
			var newName = ToPascalCase(name);

			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is not IMethodSymbol methodSymbol)
			{
				return document.Project.Solution;
			}

			SymbolRenameOptions renameOptions = new()
			{
				RenameOverloads = false,
				RenameInStrings = false,
				RenameInComments = false,
				RenameFile = false,
			};

			Solution newSolution = await Renamer.RenameSymbolAsync(
				document.Project.Solution,
				methodSymbol,
				renameOptions,
				newName,
				cancellationToken).ConfigureAwait(false);

			return newSolution;
		}

		private static string ToPascalCase(string name)
		{
			var parts = name.Split(UnderscoreSeparator, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0)
			{
				return name;
			}
			for (var i = 0; i < parts.Length; i++)
			{
				parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
			}
			return string.Join(string.Empty, parts);
		}
	}
}
