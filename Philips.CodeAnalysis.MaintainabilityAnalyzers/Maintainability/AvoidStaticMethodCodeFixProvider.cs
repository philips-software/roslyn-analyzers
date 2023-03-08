// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidStaticMethodAnalyzer)), Shared]
	public class AvoidStaticMethodCodeFixProvider : SingleDiagnosticCodeFixProvider<MethodDeclarationSyntax>
	{
		protected override string Title => "Remove static modifier from method";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidStaticMethods;

		protected override async Task<Document> ApplyFix(Document document, MethodDeclarationSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxToken modifierToRemove = node.Modifiers.First(m => m.ValueText == @"static");
			SyntaxTokenList newModifiers = node.Modifiers.Remove(modifierToRemove);

			MethodDeclarationSyntax newMethod = node.WithModifiers(newModifiers);
			SyntaxNode newRoot = rootNode.ReplaceNode(node, newMethod);
			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}
