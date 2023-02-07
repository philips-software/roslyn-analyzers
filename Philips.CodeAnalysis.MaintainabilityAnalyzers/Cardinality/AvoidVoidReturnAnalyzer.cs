// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;
using static LanguageExt.Prelude;
using static Philips.CodeAnalysis.MaintainabilityAnalyzers.Cardinality.MethodPredicates;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Cardinality
{

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidVoidReturnAnalyzer : DiagnosticAnalyzer
	{

		private const string Title = @"Method returns void";
		private const string MessageFormat = @"Method {0} returns void";
		private const string Description = @"Void returns imply a hidden side effect, since there is otherwise a singularly unique unit function.";
		private const string Category = "Functional Programming";

		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidVoidReturn), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		private static readonly Action<SyntaxNodeAnalysisContext> ReportAnalysisAction =
			(cntx) => AnalyzeMethod(cntx.Node as MethodDeclarationSyntax).Iter(cntx.ReportDiagnostic);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(ReportAnalysisAction, SyntaxKind.MethodDeclaration);
		}

		public static Option<Diagnostic> AnalyzeMethod(MethodDeclarationSyntax method)
		{
			return Optional(method)
				.Filter(IsNotOverridenMethod)
				.Select(MethodReturnType)
				.Filter((p) => p.ReturnType?.Keyword.IsKind(SyntaxKind.VoidKeyword) ?? false)
				.Select(AnalyzeReturnType);
		}

		public static Diagnostic AnalyzeReturnType((SyntaxToken MethodId, PredefinedTypeSyntax ReturnType) p)
		{
			return Diagnostic.Create(Rule, p.MethodId.GetLocation(), p.MethodId.Text);
		}
	}
}
