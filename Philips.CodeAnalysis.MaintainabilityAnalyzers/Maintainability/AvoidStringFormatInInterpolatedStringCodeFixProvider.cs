// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidStringFormatInInterpolatedStringCodeFixProvider)), Shared]
	public class AvoidStringFormatInInterpolatedStringCodeFixProvider : SingleDiagnosticCodeFixProvider<InterpolationSyntax>
	{
		protected override string Title => "Simplify string.Format in interpolated string";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidStringFormatInInterpolatedString;

		protected override Task<Document> ApplyFix(Document document, InterpolationSyntax node,
			ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			// TODO: Implement the actual logic to convert string.Format to direct interpolation
			// For now, just return the document unchanged
			return Task.FromResult(document);
		}
	}
}
