// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Report when a class or method has the MethodImplAttribute with the option
	/// MethodImplOptions.Synchronized.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidMethodImplSynchronizedAnalyzer : DiagnosticAnalyzer
	{
		private const string Title =
			"Avoid using [MethodImpl(MethodImplOptions.Synchorinzed] on methods.";
		private const string MethodMessage =
			"Method {0} should not have the MethodImplOptions.Synchronized.";
		private const string Description =
			"Avoid using [MethodImpl(MethodImplOptions.Synchronized)] on methods.";
		private const string Category = "Usage";

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(
				Helper.ToDiagnosticId(DiagnosticIds.AvoidMethodImplSynchronized),
				Title,
				MethodMessage,
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
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
		}

		private void AnalyzeSymbol(SymbolAnalysisContext context)
		{
			var symbol = context.Symbol;
			var attributes = symbol.GetAttributes();
			if (attributes.Any())
			{
				var methodImpl = attributes.Where(IsMethodImplAttributeWithSynchronizedOption);
				if (methodImpl.Any())
				{
					var diagnostic = Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		private bool IsMethodImplAttributeWithSynchronizedOption(AttributeData attr)
		{
			bool isMethodImpl = false;
			if (
				attr.AttributeClass != null &&
				attr.AttributeClass.Name == "MethodImplAttribute" &&
				!attr.ConstructorArguments.IsEmpty
			)
			{
				var argument = attr.ConstructorArguments.First();
				// Check is Synchronized flag is set (value is 32).
				var isSynchronized = ((int)argument.Value & 32) != 0;
				isMethodImpl = isSynchronized;
			}
			return isMethodImpl;
		}
	}
}
