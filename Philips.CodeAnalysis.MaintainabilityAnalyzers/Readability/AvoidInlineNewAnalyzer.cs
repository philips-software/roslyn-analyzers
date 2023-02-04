// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidInlineNewAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Do not inline new T() calls";
		private const string MessageFormat = @"Do not inline the constructor call for class {0}";
		private const string Description = @"Create a local variable, or a field for the temporary instance of class '{0}'";
		private const string Category = Categories.Readability;
		private static readonly HashSet<string> AllowedMethods = new() { "ToString", "ToList", "ToArray", "AsSpan" };

		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidInlineNew), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			ObjectCreationExpressionSyntax oce = (ObjectCreationExpressionSyntax)context.Node;

			SyntaxNode parent = oce.Parent;

			if (!IsInlineNew(parent))
			{
				return;
			}

			if (IsCallingAllowedMethod(parent))
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, oce.GetLocation(), oce.Type.ToString()));
		}

		private static bool IsInlineNew(SyntaxNode node)
		{
			return 
				node is MemberAccessExpressionSyntax || 
				(node is ParenthesizedExpressionSyntax syntax && IsInlineNew(syntax.Parent));
		}

		private static bool IsCallingAllowedMethod(SyntaxNode node)
		{
			if (node is ParenthesizedExpressionSyntax syntax)
			{
				return IsCallingAllowedMethod(syntax.Parent);
			}
			return
				node is MemberAccessExpressionSyntax memberAccess &&
				AllowedMethods.Contains(memberAccess.Name.Identifier.Text);
		}
	}
}
