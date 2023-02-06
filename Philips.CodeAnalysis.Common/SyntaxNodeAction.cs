// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public abstract class SyntaxNodeAction<T> where T : SyntaxNode
	{
		public SyntaxNodeAnalysisContext Context { get; set; } // todo: init
		public T Node { get; set; }
		public DiagnosticDescriptor Rule { get; set; }

		protected Helper Helper { get; } = new Helper();

		public abstract void Analyze();

		public void ReportDiagnostic(Location location = null, params object[] messageArgs)
		{
			Diagnostic diagnostic = Diagnostic.Create(Rule, location, messageArgs);
			Context.ReportDiagnostic(diagnostic);
		}
	}
}
