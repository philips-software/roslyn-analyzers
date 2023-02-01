// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PassByRefAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid passing parameters by reference";
		public const string MessageFormat = @"Parameter '{0}' is never written to. It should not be passed by reference.";
		private const string Description = @"There is no need to pass parameters by reference if the method does not write to them.";
		private const string Category = Categories.Maintainability;

		public DiagnosticDescriptor Rule { get; } = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidPassByReference), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(OnMethod, SyntaxKind.MethodDeclaration);
		}

		private void OnMethod(SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

			if (methodDeclarationSyntax.ParameterList is null || methodDeclarationSyntax.ParameterList.Parameters.Count == 0)
			{
				return;
			}

			bool? isInterfaceMethod = null;
			foreach (ParameterSyntax parameterSyntax in methodDeclarationSyntax.ParameterList.Parameters)
			{
				if (!parameterSyntax.Modifiers.Any(SyntaxKind.RefKeyword))
				{
					continue;
				}

				if (IsWrittenTo(context, methodDeclarationSyntax, parameterSyntax))
				{
					continue;
				}

				if (!isInterfaceMethod.HasValue)
				{
					isInterfaceMethod = IsInterfaceOrBaseClassMethod(context, methodDeclarationSyntax);
				}

				if (isInterfaceMethod.Value)
				{
					return;
				}

				context.ReportDiagnostic(Diagnostic.Create(Rule, parameterSyntax.GetLocation(), parameterSyntax.Identifier));
			}
		}

		private bool IsInterfaceOrBaseClassMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclarationSyntax)
		{
			var semanticModel = context.SemanticModel;

			var method = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);

			if (method.IsOverride)
			{
				return true;
			}

			if (method.ExplicitInterfaceImplementations.Length != 0)
			{
				return true;
			}

			foreach (var iface in method.ContainingType.AllInterfaces)
			{
				foreach (var candidateInterfaceMethod in iface.GetMembers(method.Name))
				{
					var result = method.ContainingType.FindImplementationForInterfaceMember(candidateInterfaceMethod);

					if (result != null && SymbolEqualityComparer.Default.Equals(result, method))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool IsWrittenTo(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclarationSyntax, ParameterSyntax parameterSyntax)
		{
			var semanticModel = context.SemanticModel;

			var targetSymbol = semanticModel.GetDeclaredSymbol(parameterSyntax);

			if (targetSymbol is null)
			{
				return true;
			}

			DataFlowAnalysis flow;
			if (methodDeclarationSyntax.Body != null)
			{
				if (!methodDeclarationSyntax.Body.Statements.Any())
				{
					return false;
				}

				var firstStatement = methodDeclarationSyntax.Body.Statements.First();
				var lastStatement = methodDeclarationSyntax.Body.Statements.Last();
				flow = semanticModel.AnalyzeDataFlow(firstStatement, lastStatement);
			}
			else if (methodDeclarationSyntax.ExpressionBody != null)
			{
				flow = semanticModel.AnalyzeDataFlow(methodDeclarationSyntax.ExpressionBody.Expression);
			}
			else
			{
				return true;
			}

			bool isWrittenInside = flow.WrittenInside.Contains(targetSymbol);

			return isWrittenInside;
		}
	}
}
