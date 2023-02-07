// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public abstract class SyntaxNodeAction<T> where T : SyntaxNode
	{
		public SyntaxNodeAnalysisContext Context { get; init; }
		public T Node { get; init; }
		public DiagnosticDescriptor Rule { get; init; }
		public DiagnosticAnalyzer Analyzer { get; init; }

		protected Helper Helper { get; init; } = new Helper();

		public abstract IEnumerable<Diagnostic> Analyze();

		public Diagnostic PrepareDiagnostic(Location location = null, params object[] messageArgs)
		{
			return Diagnostic.Create(Rule, location, messageArgs);
		}
	}
}

// In order to use init above: https://developercommunity.visualstudio.com/t/error-cs0518-predefined-type-systemruntimecompiler/1244809
namespace System.Runtime.CompilerServices
{
	internal static class IsExternalInit { }
}
