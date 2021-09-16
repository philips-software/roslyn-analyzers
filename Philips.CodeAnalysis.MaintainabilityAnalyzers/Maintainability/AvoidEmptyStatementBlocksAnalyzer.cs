// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;


namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]

	public class AvoidEmptyStatementBlocksAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid empty statement blocks";
		public const string MessageFormat = @"Avoid empty statement blocks";
		private const string Description = @"Avoid empty statement blocks";
		private const string Category = Categories.Maintainability;

		private const string CatchBlockTitle = @"Avoid empty catch blocks";
		public const string CatchBlockMessageFormat = @"Avoid empty catch blocks";
		private const string CatchBlockDescription = @"Avoid empty catch blocks";


		public DiagnosticDescriptor StatementRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidEmptyStatementBlock), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		public DiagnosticDescriptor CatchRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidEmptyCatchBlock), CatchBlockTitle, CatchBlockMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: CatchBlockDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(StatementRule, CatchRule); } }
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Block);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			BlockSyntax blockSyntax = (BlockSyntax)context.Node;

			if (blockSyntax.Statements == null || blockSyntax.Statements.Any())
			{
				return;
			}

			// Empty constructors are acceptable
			if (blockSyntax.Parent is ConstructorDeclarationSyntax)
			{
				return;
			}

			// Empty public or protected methods are acceptable, as it could be part of an API, or an interface implementation
			if (blockSyntax.Parent is MethodDeclarationSyntax methodSynxtax)
			{
				if (methodSynxtax.Modifiers.Any(SyntaxKind.PublicKeyword) || methodSynxtax.Modifiers.Any(SyntaxKind.ProtectedKeyword))
				{
					return;
				}
			}

			// Empty catch blocks are a different type of code smell.
			if (blockSyntax.Parent is CatchClauseSyntax)
			{
				Diagnostic emptyCatchDiagnostic = Diagnostic.Create(CatchRule, blockSyntax.GetLocation());
				context.ReportDiagnostic(emptyCatchDiagnostic);
				return;
			}

			// ParanthesizedLambdaExpressions are acceptable () => { }, until a pre-canned static "EmptyAction" is defined.
			if (blockSyntax.Parent is ParenthesizedLambdaExpressionSyntax || blockSyntax.Parent is SimpleLambdaExpressionSyntax)
			{
				return;
			}

			// Empty lock blocks are acceptable.  lock (x) {}
			if (blockSyntax.Parent is LockStatementSyntax)
			{
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(StatementRule, blockSyntax.GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}