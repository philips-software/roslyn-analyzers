// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	/// <summary>
	/// Base class for an <see cref="DiagnosticAnalyzer"/> which may report only a single <see cref="DiagnosticDescriptor"/>.
	/// </summary>
	public abstract class SingleDiagnosticAnalyzer : DiagnosticAnalyzerBase
	{
		public DiagnosticId DiagnosticId { get; }
		public string Id { get; }
		protected DiagnosticDescriptor Rule { get; }

		protected SingleDiagnosticAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
			DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = true)
		{
			DiagnosticId = id;
			Id = id.ToId();
			var helpLink = id.ToHelpLinkUrl();
			Rule = new(Id, title, messageFormat, category, severity, isEnabled, description, helpLink);
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	}
}
