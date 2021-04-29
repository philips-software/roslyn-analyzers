// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Diagnostic for using threads from the <see cref="System.Threading.ThreadPool"/> exclusively.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidThreadStartAnalyzer : MemberInClassAnalyzerBase
	{
		private const string Title = "Avoid creating threads directly.";
		private const string Message = "Thread {0} should be taken from the ThreadPool.";
		private const string Description = "Avoid creating threads directly.";
		private const string Category = Categories.RuntimeFailure;

		/// <summary>
		/// Constructor.
		/// </summary>
		public AvoidThreadStartAnalyzer() : base("System.Threading.Thread", ".ctor")
		{
		}

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.AvoidThreadStart),
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
