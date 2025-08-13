// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.SecurityAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidPkcsPaddingWithRsaEncryptionCodeFixProvider)), Shared]
	public class AvoidPkcsPaddingWithRsaEncryptionCodeFixProvider : SingleDiagnosticCodeFixProvider<MemberAccessExpressionSyntax>
	{
		protected override string Title => "Replace with OaepSHA256 padding";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidPkcsPaddingWithRsaEncryption;

		protected override async Task<Document> ApplyFix(Document document, MemberAccessExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			// Replace .Pkcs1 with .OaepSHA256
			IdentifierNameSyntax newIdentifier = SyntaxFactory.IdentifierName("OaepSHA256");
			MemberAccessExpressionSyntax newMemberAccess = node.WithName(newIdentifier);

			SyntaxNode newNode = newMemberAccess.WithAdditionalAnnotations(Formatter.Annotation);
			rootNode = rootNode.ReplaceNode(node, newNode);

			return document.WithSyntaxRoot(rootNode);
		}
	}
}