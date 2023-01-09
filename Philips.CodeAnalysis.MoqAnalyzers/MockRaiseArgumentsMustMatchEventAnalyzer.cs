// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MoqAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MockRaiseArgumentsMustMatchEventAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Mock<T>.Raise(x => x.Event += null, sender, args) must have correct parameters";
		private const string MessageFormatTypeMismatch = @"Parameter '{0}' ({1}) does not match expected type '{2}'";
		private const string MessageFormatArgumentCount = @"Argument count mismatch";
		private const string Description = @"Parameter mismatch";
		private const string Category = Categories.RuntimeFailure;

		private static readonly DiagnosticDescriptor TypeMismatchRule = new(Helper.ToDiagnosticId(DiagnosticIds.MockRaiseArgumentsMustMatchEvent), Title, MessageFormatTypeMismatch, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		private static readonly DiagnosticDescriptor ArgumentCountRule = new(Helper.ToDiagnosticId(DiagnosticIds.MockRaiseArgumentCountMismatch), Title, MessageFormatArgumentCount, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(TypeMismatchRule, ArgumentCountRule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("Moq.MockRepository") == null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction(AnalyzeInstanceCall, SyntaxKind.InvocationExpression);
			});
		}

		private void AnalyzeInstanceCall(SyntaxNodeAnalysisContext context)
		{
			InvocationExpressionSyntax invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;

			if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
			{
				return;
			}

			if (memberAccessExpressionSyntax.Name is not IdentifierNameSyntax identifierNameSyntax)
			{
				return;
			}

			switch (identifierNameSyntax.Identifier.ValueText)
			{
				case @"Raise": AnalyzeInvocation(context, invocationExpressionSyntax); break;
				default:
					return;
			}
		}

		private void AnalyzeInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax)
		{
			if (invocationExpressionSyntax.ArgumentList == null)
			{
				return;
			}

			if (invocationExpressionSyntax.ArgumentList.Arguments.Count == 0)
			{
				return;
			}

			if (invocationExpressionSyntax.ArgumentList.Arguments[0].Expression is not SimpleLambdaExpressionSyntax lambdaExpressionSyntax)
			{
				return;
			}

			if (lambdaExpressionSyntax.Body == null)
			{
				return;
			}

			if (lambdaExpressionSyntax.Body is not AssignmentExpressionSyntax assignmentExpressionSyntax)
			{
				return;
			}

			if (assignmentExpressionSyntax.Left is not MemberAccessExpressionSyntax accessExpressionSyntax)
			{
				return;
			}

			var symbolInfo = context.SemanticModel.GetSymbolInfo(accessExpressionSyntax);

			if (symbolInfo.Symbol == null)
			{
				return;
			}

			if (symbolInfo.Symbol is not IEventSymbol eventSymbol)
			{
				return;
			}

			if (eventSymbol.Type is not INamedTypeSymbol namedTypeSymbol)
			{
				return;
			}

			SymbolInfo raiseSymbol = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax.Expression);

			if (raiseSymbol.Symbol == null || raiseSymbol.Symbol is not IMethodSymbol raiseMethodSymbol)
			{
				return;
			}

			int firstArgument = 0;
			int argumentsToCheck;
			if (raiseMethodSymbol.Parameters.Last().IsParams)
			{
				argumentsToCheck = invocationExpressionSyntax.ArgumentList.Arguments.Count - 1;

				if (namedTypeSymbol.DelegateInvokeMethod.Parameters.Length != argumentsToCheck)
				{
					context.ReportDiagnostic(Diagnostic.Create(ArgumentCountRule, invocationExpressionSyntax.GetLocation()));
					return;
				}
			}
			else
			{
				bool isError = false;
				//it has a single eventargs argument.  Compiler has made sure that the types are the same, make sure the delegate takes object sender, event args
				if (namedTypeSymbol.DelegateInvokeMethod.Parameters.Length != 2)
				{
					isError = true;
				}

				if (!isError && namedTypeSymbol.DelegateInvokeMethod.Parameters[0].Type.Name != "Object")
				{
					isError = true;
				}

				if (isError)
				{
					context.ReportDiagnostic(Diagnostic.Create(ArgumentCountRule, invocationExpressionSyntax.GetLocation()));
					return;
				}

				argumentsToCheck = 2;
				if (raiseMethodSymbol.Parameters.Length == 2)
				{
					firstArgument = 1;
				}
			}

			for (int i = firstArgument; i < argumentsToCheck; i++)
			{
				ArgumentSyntax argument = invocationExpressionSyntax.ArgumentList.Arguments[i + 1 - firstArgument];
				ITypeSymbol expectedType = namedTypeSymbol.DelegateInvokeMethod.Parameters[i].Type;

				SymbolInfo typeSymbol = context.SemanticModel.GetSymbolInfo(argument.Expression);
				Conversion conversion = context.SemanticModel.ClassifyConversion(argument.Expression, expectedType);

				if (!conversion.IsImplicit)
				{
					context.ReportDiagnostic(Diagnostic.Create(TypeMismatchRule, argument.GetLocation(), argument.Expression, typeSymbol.Symbol?.Name, expectedType.Name));
				}
			}
		}
	}
}
