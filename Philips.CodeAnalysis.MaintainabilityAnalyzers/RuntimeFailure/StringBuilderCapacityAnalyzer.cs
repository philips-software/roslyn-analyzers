// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Diagnostic for creating <see cref="System.Text.StringBuilder"/> with an explicitly set capacity.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class StringBuilderCapacityAnalyzer : MemberInClassAnalyzerBase
	{
		private const string Title = "Create StringBuilder with Capacity.";
		private const string Message = "Initialize StringBuilder variable {0} with Capacity.";
		private const string Description = "Create StringBuilder with Capacity.";
		private const string Category = Categories.RuntimeFailure;

		/// <summary>
		/// Constructor.
		/// </summary>
		public StringBuilderCapacityAnalyzer() : base(
			"System.Text.StringBuilder",
			".ctor",
			true,
			new[] { "System.Int32" }
		)
		{
		}

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.StringBuilderCapacity),
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
			var variableName = Helper.GetInvokedMemberIdentifier(node).Text;
			var loc = node.GetLocation();
			context.ReportDiagnostic(Diagnostic.Create(Rule, loc, variableName));
		}
	}
}
