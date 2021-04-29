// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Diagnostic to avoid calling <see cref="System.GC.WaitForPendingFinalizers()"/>.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidGcWaitForPendingFinalizersAnalyzer : MemberInClassAnalyzerBase
	{
		private const string Title = "Avoid calling GC.WaitForPendingFinalizers().";
		private const string Message = "Avoid calling GC.WaitForPendingFinalizers().";
		private const string Description = "Avoid calling GC.WaitForPendingFinalizers().";
		private const string Category = Categories.RuntimeFailure;

		/// <summary>
		/// Constructor.
		/// </summary>
		public AvoidGcWaitForPendingFinalizersAnalyzer() : base(
			"System.GC",
			"WaitForPendingFinalizers"
		)
		{
		}

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.AvoidGcWaitForPendingFinalizers),
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
