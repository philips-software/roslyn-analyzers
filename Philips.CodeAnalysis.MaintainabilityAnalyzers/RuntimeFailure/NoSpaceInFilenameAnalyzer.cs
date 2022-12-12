// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Report on file names with spaces, as these tend to cause scripting issues.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoSpaceInFilenameAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>
		/// Diagnostic for file names with spaces.
		/// </summary>
		private const string Title = "Do not use spaces in file names.";
		private const string Message = "File '{0}' has spaces in its path.";
		private const string Description = "Space in filename/ path.";
		private const string Category = Categories.RuntimeFailure;

		private static readonly DiagnosticDescriptor Rule =
			new(
				Helper.ToDiagnosticId(DiagnosticIds.NoSpaceInFilename),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: Description
			);

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Rule);

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxTreeAction(AnalyzeTree);
		}

		private void AnalyzeTree(SyntaxTreeAnalysisContext context)
		{
			var filePath = context.Tree.FilePath;

			if (Helper.IsGeneratedCode(filePath))
			{
				return;
			}

			if (filePath.IndexOf(' ') != -1)
			{
				var location = Location.Create(context.Tree, TextSpan.FromBounds(0, 0));
				var diagnostic = Diagnostic.Create(Rule, location, filePath);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
