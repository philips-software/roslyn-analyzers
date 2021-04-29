// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Diagnostic to avoid calling <see cref="System.GC.Collect()"/> directly.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidGcCollectAnalyzer : MemberInClassAnalyzerBase
	{
		private const string Title = "Avoid explicitly call GC.Collect().";
		private const string Message = "Avoid calling GC.Collect() explicitly.";
		private const string Description = "Avoid explicitly call GC.Collect().";
		private const string Category = Categories.RuntimeFailure;

		/// <summary>
		/// Constructor.
		/// </summary>
		public AvoidGcCollectAnalyzer() : base("System.GC", "Collect")
		{
		}

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.AvoidGcCollect),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: Description
			);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		protected override void OnReportDiagnostic(
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax node
		)
		{
			var location = node.GetLocation();
			context.ReportDiagnostic(Diagnostic.Create(Rule, location));
		}
	}
}
