// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

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

				Location location = parameterSyntax.GetLocation();
				ReportDiagnostic(location, parameterSyntax.Identifier);
			}
		}

		private bool IsInterfaceOrBaseClassMethod()
		{
			SemanticModel semanticModel = Context.SemanticModel;

			IMethodSymbol method = semanticModel.GetDeclaredSymbol(Node);

			if (method.IsOverride)
			{
				return true;
			}

			if (method.ExplicitInterfaceImplementations.Length != 0)
			{
				return true;
			}

			foreach (INamedTypeSymbol iface in method.ContainingType.AllInterfaces)
			{
				foreach (ISymbol candidateInterfaceMethod in iface.GetMembers(method.Name))
				{
					ISymbol result = method.ContainingType.FindImplementationForInterfaceMember(candidateInterfaceMethod);

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
			SemanticModel semanticModel = Context.SemanticModel;

			IParameterSymbol targetSymbol = semanticModel.GetDeclaredSymbol(parameterSyntax);

			if (targetSymbol is null)
			{
				return true;
			}

			DataFlowAnalysis flow = DataFlowHelper.GetDataFlowAnalysis(Context.SemanticModel, Node);
			if (flow is null)
			{
				return true;
			}

			var isWrittenInside = flow.WrittenInside.Contains(targetSymbol);

			return isWrittenInside;
		}
	}
}
