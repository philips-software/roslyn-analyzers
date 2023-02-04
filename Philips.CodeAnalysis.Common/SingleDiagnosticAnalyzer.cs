// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public abstract class SingleDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		public DiagnosticId DiagnosticId { get; }

		public DiagnosticDescriptor Rule { get; }

		protected SingleDiagnosticAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
											DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = true)
		{
			DiagnosticId = id;
			Rule = new(Helper.ToDiagnosticId(id), title, messageFormat, category, severity, isEnabled, description);
		}
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	}
}
