// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	/// <summary>
	/// Report on file names that are too long for windows to handle properly.
	/// See <seealso cref="https://docs.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation"/>
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LimitPathLengthAnalyzer : DiagnosticAnalyzer
	{
		private const string Title =
			"Path too long, make smaller to avoid $MAX_RELATIVE_PATH_LENGTH limit.";
		private const string Message = "File {0} has too long path.";
		private const string Description = "Too long path.";
		private const string Category = Categories.RuntimeFailure;

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.LimitPathLength),
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
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxTreeAction(AnalyzeTree);
		}

		private void AnalyzeTree(SyntaxTreeAnalysisContext context)
		{
			var filePath = context.Tree.FilePath;
			if (filePath.Length > 260)
			{
				var location = Location.Create(context.Tree, TextSpan.FromBounds(0, 0));
				var diagnostic = Diagnostic.Create(Rule, location, Path.GetFileName(filePath));
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
