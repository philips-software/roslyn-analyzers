// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidImplementingFinalizersAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid implementing a finalizer";
		private const string MessageFormat = @"Avoid implement a finalizer, use Dispose instead.";
		private const string Description = @"Avoid implement a finalizer, use Dispose instead. If the class has unmanaged fields, finalizers are allowed if they only call Dispose.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidImplementingFinalizers),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.DestructorDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var finalizer = (DestructorDeclarationSyntax)context.Node;
			var body = finalizer.Body;
			var children = body != null ? body.ChildNodes() : Array.Empty<SyntaxNode>();
			if (children.Any() && children.All(IsDisposeCall))
			{
				return;
			}
			var loc = finalizer.GetLocation();
			context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
		}

		private static bool IsDisposeCall(SyntaxNode node)
		{
			if (node is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation })
			{
				return invocation.Expression.ToString() == "Dispose";
			}

			return false;
		}
	}
}
