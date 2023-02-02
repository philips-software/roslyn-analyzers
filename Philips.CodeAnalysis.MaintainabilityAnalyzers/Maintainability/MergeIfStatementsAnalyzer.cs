// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Philips.CodeAnalysis.Common;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MergeIfStatementsAnalyzer : DiagnosticAnalyzer
	{
		private readonly GeneratedCodeAnalysisFlags _generatedCodeFlags;

		private const string TitleFormat = "Merge If Statements";
		private const string MessageFormat = "Merge If Statements";
		private const string DescriptionFormat = "Merging If statement with outer If statement to reduce cognitive load";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(
				Helper.ToDiagnosticId(DiagnosticId.MergeIfStatements), TitleFormat, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, DescriptionFormat);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public MergeIfStatementsAnalyzer()
			: this(GeneratedCodeAnalysisFlags.None)
		{ }

		public MergeIfStatementsAnalyzer(GeneratedCodeAnalysisFlags generatedCodeFlags)
		{
			_generatedCodeFlags = generatedCodeFlags;
		}


		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(_generatedCodeFlags);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IfStatement);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			IfStatementSyntax ifStatementSyntax = (IfStatementSyntax)context.Node;

			// Node has an else clause
			if (ifStatementSyntax.Else != null)
			{
				return;
			}

			var parent = ifStatementSyntax.Parent;

			if (parent is BlockSyntax parentBlockSyntax)
			{
				// Has multiple statements in the block
				if (parentBlockSyntax.Statements.Count > 1)
				{
					return;
				}

				parent = parentBlockSyntax.Parent;
			}

			// Parent is not an If statement
			if (parent is not IfStatementSyntax parentIfSyntax)
			{
				return;
			}

			// Parent has an else clause
			if (parentIfSyntax.Else != null)
			{
				return;
			}

			// Has ||
			if (IfConditionHasLogicalAnd(ifStatementSyntax))
			{
				return;
			}

			// Parent has ||
			if (IfConditionHasLogicalAnd(parentIfSyntax))
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, ifStatementSyntax.IfKeyword.GetLocation()));
		}

		private bool IfConditionHasLogicalAnd(IfStatementSyntax ifStatement)
		{
			return ifStatement.Condition.DescendantTokens().Any((token) => { return token.Kind() == SyntaxKind.BarBarToken; });
		}
	}
}
