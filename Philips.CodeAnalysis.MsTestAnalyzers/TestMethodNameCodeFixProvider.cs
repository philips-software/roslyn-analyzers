// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestMethodNameCodeFixProvider)), Shared]
	public class TestMethodNameCodeFixProvider : SolutionCodeFixProvider<MethodDeclarationSyntax>
	{
		protected override string Title => "Remove invalid prefix";

		protected override DiagnosticId DiagnosticId => DiagnosticId.TestMethodName;

		protected override async Task<Solution> ApplyFix(Document document, MethodDeclarationSyntax node, CancellationToken cancellationToken)
		{
			// Compute new name.
			var name = node.Identifier.Text;

			while (name.Contains(StringConstants.TestAttributeName))
			{
				name = name.Replace(StringConstants.TestAttributeName, string.Empty);
			}
			while (name.Contains(StringConstants.EnsureAttributeName))
			{
				name = name.Replace(StringConstants.EnsureAttributeName, string.Empty);
			}
			while (name.Contains(StringConstants.VerifyAttributeName))
			{
				name = name.Replace(StringConstants.VerifyAttributeName, string.Empty);
			}

			name += StringConstants.TestAttributeName;

			// Get the symbol representing the method to be renamed.
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			IMethodSymbol typeSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

			SymbolRenameOptions renameOptions = new()
			{
				RenameOverloads = false,
				RenameInStrings = false,
				RenameInComments = false,
				RenameFile = false,
			};

			Solution newSolution = await Renamer.RenameSymbolAsync(
				document.Project.Solution,
				typeSymbol,
				renameOptions,
				name,
				cancellationToken).ConfigureAwait(false);

			return newSolution;
		}
	}
}
