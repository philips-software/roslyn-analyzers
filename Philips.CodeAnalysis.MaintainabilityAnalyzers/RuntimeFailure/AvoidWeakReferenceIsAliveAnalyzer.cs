// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Diagnostic for calling <see cref="System.WeakReference.IsAlive"/>.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidWeakReferenceIsAliveAnalyzer : MemberInClassAnalyzerBase
	{
		private const string Title = "Avoid calling WeakReference.IsAlive.";
		private const string Message =
			"Calling IsAlive on variable {0} which is of type WeakReference.";
		private const string Description = "Avoid calling WeakReference.IsAlive.";
		private const string Category = Categories.RuntimeFailure;

		/// <summary>
		/// Constructor.
		/// </summary>
		public AvoidWeakReferenceIsAliveAnalyzer() : base("System.WeakReference", "IsAlive")
		{
		}

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.AvoidWeakReferenceIsAlive),
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
			context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), variableName));
		}
	}
}
