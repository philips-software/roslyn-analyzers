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
		private const string Category = Categories.Maintainability;

		private const string BlockTitle = @"Avoid empty statement blocks";
		public const string BlockMessageFormat = BlockTitle;
		private const string BlockDescription = BlockTitle;

		private const string CatchBlockTitle = @"Avoid empty catch blocks";
		public const string CatchBlockMessageFormat = CatchBlockTitle;
		private const string CatchBlockDescription = CatchBlockTitle;

		private const string StatementTitle = @"Avoid empty statements";
		public const string StatementMessageFormat = StatementTitle;
		private const string StatementDescription = StatementTitle;

		public DiagnosticDescriptor BlockRule { get; } = new(Helper.ToDiagnosticId(DiagnosticId.AvoidEmptyStatementBlock), BlockTitle, BlockMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: BlockDescription);
		public DiagnosticDescriptor CatchRule { get; } = new(Helper.ToDiagnosticId(DiagnosticId.AvoidEmptyCatchBlock), CatchBlockTitle, CatchBlockMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: CatchBlockDescription);
		public DiagnosticDescriptor StatementRule { get; } = new(Helper.ToDiagnosticId(DiagnosticId.AvoidEmptyStatement), StatementTitle, StatementMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: StatementDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(BlockRule, CatchRule, StatementRule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Block);
			context.RegisterSyntaxNodeAction(AnalyzeEmptyStatement, SyntaxKind.EmptyStatement);
		}

		private void AnalyzeEmptyStatement(SyntaxNodeAnalysisContext context)
		{
			Location resultOfGetLocation = context.Node.GetLocation();
			var diagnostic = Diagnostic.Create(StatementRule, resultOfGetLocation);
			context.ReportDiagnostic(diagnostic);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is not BlockSyntax blockSyntax)
			{
				return;
			}

			if (blockSyntax.Statements.Any())
			{
				return;
			}

			// Empty constructors are acceptable
			if (blockSyntax.Parent is ConstructorDeclarationSyntax)
			{
				return;
			}

			// Empty public or protected members are acceptable, as it could be part of an API, or an interface implementation
			if (blockSyntax.Parent is MemberDeclarationSyntax memberSyntax && memberSyntax.IsCallableFromOutsideClass())
			{
				return;
			}

			// Empty catch blocks are a different type of code smell.
			if (blockSyntax.Parent is CatchClauseSyntax)
			{
				Location resultOfGetLocation = blockSyntax.GetLocation();
				var emptyCatchDiagnostic = Diagnostic.Create(CatchRule, resultOfGetLocation);
				context.ReportDiagnostic(emptyCatchDiagnostic);
				return;
			}

			// ParanthesizedLambdaExpressions are acceptable () => { }, until a pre-canned static "EmptyAction" is defined.
			if (blockSyntax.Parent is ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax)
			{
				return;
			}

			// Empty lock blocks are acceptable.
			if (blockSyntax.Parent is LockStatementSyntax)
			{
				return;
			}

			Location location = blockSyntax.GetLocation();
			var diagnostic = Diagnostic.Create(BlockRule, location);
			context.ReportDiagnostic(diagnostic);
		}
	}
}
