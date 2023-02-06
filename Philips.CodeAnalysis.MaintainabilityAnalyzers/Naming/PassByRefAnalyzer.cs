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
	public class PassByRefAnalyzer : SingleDiagnosticAnalyzer<MethodDeclarationSyntax, PassByRefSyntaxNodeAction>
	{
		private const string Title = @"Avoid passing parameters by reference";
		public const string MessageFormat = @"Parameter '{0}' is never written to. It should not be passed by reference.";
		private const string Description = @"There is no need to pass parameters by reference if the method does not write to them.";

		public PassByRefAnalyzer()
			: base(DiagnosticId.AvoidPassByReference, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class PassByRefSyntaxNodeAction : SyntaxNodeAction<MethodDeclarationSyntax>
	{
		public override void Analyze()
		{
			if (Node.ParameterList is null || Node.ParameterList.Parameters.Count == 0)
			{
				return;
			}

			bool? isInterfaceMethod = null;
			foreach (ParameterSyntax parameterSyntax in Node.ParameterList.Parameters)
			{
				if (!parameterSyntax.Modifiers.Any(SyntaxKind.RefKeyword))
				{
					continue;
				}

				if (IsWrittenTo(parameterSyntax))
				{
					continue;
				}

				if (!isInterfaceMethod.HasValue)
				{
					isInterfaceMethod = IsInterfaceOrBaseClassMethod();
				}

				if (isInterfaceMethod.Value)
				{
					return;
				}

				ReportDiagnostic(parameterSyntax.GetLocation(), parameterSyntax.Identifier);
			}
		}

		private bool IsInterfaceOrBaseClassMethod()
		{
			var semanticModel = Context.SemanticModel;

			var method = semanticModel.GetDeclaredSymbol(Node);

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

		private bool IsWrittenTo(ParameterSyntax parameterSyntax)
		{
			var semanticModel = Context.SemanticModel;

			var targetSymbol = semanticModel.GetDeclaredSymbol(parameterSyntax);

			if (targetSymbol is null)
			{
				return true;
			}

			DataFlowAnalysis flow;
			if (Node.Body != null)
			{
				if (!Node.Body.Statements.Any())
				{
					return false;
				}

				var firstStatement = Node.Body.Statements.First();
				var lastStatement = Node.Body.Statements.Last();
				flow = semanticModel.AnalyzeDataFlow(firstStatement, lastStatement);
			}
			else if (Node.ExpressionBody != null)
			{
				flow = semanticModel.AnalyzeDataFlow(Node.ExpressionBody.Expression);
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
