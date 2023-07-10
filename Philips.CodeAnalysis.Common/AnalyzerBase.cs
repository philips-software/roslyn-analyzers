// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	/// <summary>
	/// Base class for <see cref="DiagnosticAnalyzer"/>.
	/// </summary>
	public abstract class AnalyzerBase : DiagnosticAnalyzer
	{
		public sealed override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GetGeneratedCodeAnalysisFlags());
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(startContext =>
			{
				Helper = new Helper(startContext.Options, startContext.Compilation);
				InitializeCompilation(startContext);
			});
		}

		public Helper Helper { get; private set; }

		protected abstract void InitializeCompilation(CompilationStartAnalysisContext context);

		protected virtual GeneratedCodeAnalysisFlags GetGeneratedCodeAnalysisFlags()
		{
			return GeneratedCodeAnalysisFlags.None;
		}
	}
}
