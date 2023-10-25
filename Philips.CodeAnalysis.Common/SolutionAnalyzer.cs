// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	/// <summary>
	/// Base class for <see cref="DiagnosticAnalyzer"/> which operate on the entire solution <see cref="Compilation"/>.
	/// </summary>
	public abstract class SolutionAnalyzer : DiagnosticAnalyzer
	{
		public DiagnosticId Id { get; }
		protected DiagnosticDescriptor Rule { get; }

		/// <summary>
		/// SolutionAnalyzers typically require .globalconfig file, which may require more cognitive load, limiting adoption; therefore, opt-in rather than Opt-out.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="title"></param>
		/// <param name="messageFormat"></param>
		/// <param name="description"></param>
		/// <param name="category"></param>
		/// <param name="severity"></param>
		/// <param name="isEnabled"></param>
		protected SolutionAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
											DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = false)
		{
			Id = id;
			var helpLink = id.ToHelpLinkUrl();
			Rule = new(Id.ToId(), title, messageFormat, category, severity, isEnabled, description, helpLink);
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	}
}
