// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.Test.Helpers
{
	public class TestDiagnosticProvider : FixAllContext.DiagnosticProvider
	{
		private readonly IReadOnlyList<Diagnostic> _diagnostics;

		public TestDiagnosticProvider(IReadOnlyList<Diagnostic> diagnostics, Document document)
		{
			_diagnostics = diagnostics;
			Document = document;
		}

		internal Document Document { get; }

		public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
		{
			return Task.FromResult((IEnumerable<Diagnostic>)_diagnostics);
		}

		public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
		{
			var result = _diagnostics.Where(i => i.Location.GetLineSpan().Path == document.Name);
			return Task.FromResult(result);
		}

		public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
		{
			var result = _diagnostics.Where(i => !i.Location.IsInSource);
			return Task.FromResult(result);
		}
	}
}
