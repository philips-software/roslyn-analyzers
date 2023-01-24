﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidPragmaAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid Pragma Warning";
		public const string MessageFormat = @"Do not use #pragma warning";
		private const string Description = @"Do not use #pragma warning";
		private const string Category = Categories.Maintainability;

		public List<DiagnosticDescriptor> Rules { get; } = new()
		{
			new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidPragma), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description),
		};

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { var rulesArray = Rules.ToArray(); return ImmutableArray.Create(rulesArray); } }

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.PragmaWarningDirectiveTrivia);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is not PragmaWarningDirectiveTriviaSyntax pragma)
			{
				return;
			}

			string myOwnId = Helper.ToDiagnosticId(DiagnosticIds.AvoidPragma);
			if (pragma.ErrorCodes.Where(e => e.IsKind(SyntaxKind.IdentifierName)).Any(i => i.ToString().Contains(myOwnId)))
			{
				return;
			}

			CSharpSyntaxNode violation = pragma;
			var location = violation.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rules[0], location);
			context.ReportDiagnostic(diagnostic);
		}
	}
}
