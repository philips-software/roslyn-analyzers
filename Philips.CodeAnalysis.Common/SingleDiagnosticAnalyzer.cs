// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public abstract class SingleDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		public DiagnosticId DiagnosticId { get; }
		public string Id { get; }
		protected DiagnosticDescriptor Rule { get; }

		public Helper Helper { get; private set; }

		protected SingleDiagnosticAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
											DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = true)
		{
			DiagnosticId = id;
			Id = Helper.ToDiagnosticId(id);
			var helpLink = Helper.ToHelpLinkUrl(Id);
			Rule = new(Id, title, messageFormat, category, severity, isEnabled, description, helpLink);
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		/// <summary>
		/// Boilerplate initialization for the Analyzer
		/// </summary>
		/// <exception cref="InvalidOperationException">When an Analyzer with a new type of SyntaxKind is added.</exception>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				Helper = new Helper(startContext.Options, startContext.Compilation);

				InitializeAnalysis(startContext);
			});
		}

		protected abstract void InitializeAnalysis(CompilationStartAnalysisContext context);
	}
}
