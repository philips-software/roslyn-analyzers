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

			// Get the symbol representing the type to be renamed.
			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			IMethodSymbol typeSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

			// Produce a new solution that has all references to that type renamed, including the declaration.
			Solution originalSolution = document.Project.Solution;
			Microsoft.CodeAnalysis.Options.OptionSet optionSet = originalSolution.Workspace.Options;
			Solution newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, name, optionSet, cancellationToken).ConfigureAwait(false);

			// Return the new solution with the now-uppercase type name.
			return newSolution;
		}
	}
}
