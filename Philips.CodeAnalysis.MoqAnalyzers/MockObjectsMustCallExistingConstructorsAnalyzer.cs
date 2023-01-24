﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class MockObjectsMustCallExistingConstructorsAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Mock<T> construction must call an existing constructor";
		private const string MessageFormat = @"Could not find a matching constructor for {0}";
		private const string Description = @"Could not find a constructor that matched the given arguments";
		private const string Category = Categories.RuntimeFailure;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.MockArgumentsMustMatchConstructor), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

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

				startContext.RegisterSyntaxNodeAction(AnalyzeNewObject, SyntaxKind.ObjectCreationExpression);
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

			if (memberAccessExpressionSyntax.Name is not GenericNameSyntax genericNameSyntax)
			{
				return;
			}

			switch (genericNameSyntax.Identifier.Value)
			{
				case @"Create": AnalyzeInvocation(context, invocationExpressionSyntax, "MockFactory", true, true); break;
				case @"Of": AnalyzeInvocation(context, invocationExpressionSyntax, "Mock", false, false); break;
				default:
					return;
			}
		}

		private void AnalyzeInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, string expectedClassName, bool returnsMock, bool canHaveMockBehavior)
		{
			//by now we know they are calling foo.Create<T>/foo.Of.  Drop to the semantic model, is this MockRepository.Create<T> or Mock.Of<T>?
			SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax);

			if (symbol.Symbol == null || symbol.Symbol is not IMethodSymbol method)
			{
				return;
			}

			if (method.ContainingType.Name != expectedClassName)
			{
				return;
			}

			ITypeSymbol returnType = method.ReturnType;
			if (returnsMock)
			{
				if (returnType is not INamedTypeSymbol typeSymbol || !typeSymbol.IsGenericType)
				{
					return;
				}

				returnType = typeSymbol.TypeArguments[0];
			}

			//they are calling MockRepository.Create<T>.
			VerifyMockAttempt(context, returnType, invocationExpressionSyntax.ArgumentList, canHaveMockBehavior);
		}

		private void AnalyzeNewObject(SyntaxNodeAnalysisContext context)
		{
			ObjectCreationExpressionSyntax objectCreationExpressionSyntax = (ObjectCreationExpressionSyntax)context.Node;

			if (objectCreationExpressionSyntax.Type is not GenericNameSyntax genericNameSyntax)
			{
				return;
			}

			if (genericNameSyntax.Identifier.ValueText != "Mock")
			{
				return;
			}

			SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(objectCreationExpressionSyntax);

			if (symbolInfo.Symbol == null || symbolInfo.Symbol is not IMethodSymbol mockConstructorMethod)
			{
				return;
			}


			if (mockConstructorMethod.ReceiverType is not INamedTypeSymbol typeSymbol || !typeSymbol.IsGenericType)
			{
				return;
			}

			ITypeSymbol mockedClass = typeSymbol.TypeArguments[0];

			VerifyMockAttempt(context, mockedClass, objectCreationExpressionSyntax.ArgumentList, true);
		}

		private void VerifyMockAttempt(SyntaxNodeAnalysisContext context, ITypeSymbol mockedClass, ArgumentListSyntax argumentList, bool canHaveMockBehavior)
		{
			if (mockedClass is IErrorTypeSymbol)
			{
				return;
			}

			ImmutableArray<ArgumentSyntax> arguments = ImmutableArray<ArgumentSyntax>.Empty;

			if (argumentList?.Arguments != null)
			{
				arguments = argumentList.Arguments.ToImmutableArray();
			}

			if (canHaveMockBehavior && arguments.Length > 0 && argumentList?.Arguments[0].Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
			{
				if (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax identifier && identifier.Identifier.Text == "MockBehavior")
				{
					//they passed a mock behavior as the first argument.  ignore this one, mock swallows it.
					arguments = arguments.RemoveAt(0);
				}
			}
			else if (canHaveMockBehavior && arguments.Length > 0 && argumentList?.Arguments[0].Expression is IdentifierNameSyntax identifierNameSyntax)
			{
				SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(identifierNameSyntax);

				if (symbolInfo.Symbol == null)
				{
					return;
				}

				ITypeSymbol typeSymbol = null;
				if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
				{
					typeSymbol = parameterSymbol.Type;
				}
				else if (symbolInfo.Symbol is ILocalSymbol localSymbol)
				{
					typeSymbol = localSymbol.Type;
				}
				else if (symbolInfo.Symbol is IFieldSymbol fieldSymbol)
				{
					typeSymbol = fieldSymbol.Type;
				}

				if (typeSymbol != null && typeSymbol.Name == "MockBehavior")
				{
					//they passed a mock behavior as the first argument.  ignore this one, mock swallows it.
					arguments = arguments.RemoveAt(0);
				}
			}

			switch (mockedClass.TypeKind)
			{
				case TypeKind.Interface:
				case TypeKind.Delegate:
					if (arguments.Length == 0)
					{
						return;
					}

					if (mockedClass.TypeKind == TypeKind.Delegate)
					{
						context.ReportDiagnostic(Diagnostic.Create(Rule, argumentList?.GetLocation(), argumentList));
						return;
					}
					break;
				default:
					break;
			}

			IMethodSymbol[] constructors = mockedClass.GetMembers().OfType<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Constructor && !x.IsStatic).ToArray();

			ImmutableArray<IMethodSymbol> bestFitConstructors = constructors.Where(x => x.Parameters.Length == arguments.Length).ToImmutableArray();

			if (BestFitConstructorsEmpty(bestFitConstructors, argumentList, context, out Location location))
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, argumentList));
				return;
			}

			if (!AllConstructorsFound(bestFitConstructors, arguments, context))
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, argumentList?.GetLocation(), argumentList));
			}
		}

		private bool BestFitConstructorsEmpty(ImmutableArray<IMethodSymbol> bestFitConstructors, ArgumentListSyntax argumentList, SyntaxNodeAnalysisContext context, out Location location)
		{
			if (argumentList != null)
			{
				location = argumentList.GetLocation();
			}
			else
			{
				location = context.Node.GetLocation();
			}

			return bestFitConstructors.IsEmpty;
		}

		private bool AllConstructorsFound(ImmutableArray<IMethodSymbol> bestFitConstructors, ImmutableArray<ArgumentSyntax> arguments, SyntaxNodeAnalysisContext context)
		{
			foreach (IMethodSymbol constructor in bestFitConstructors)
			{
				bool didFindAll = true;

				for (int i = 0; i < arguments.Length; i++)
				{
					ArgumentSyntax passedArgument = arguments[i];
					IParameterSymbol expectedArgumentSymbol = constructor.Parameters[i];

					Conversion c;
					if (passedArgument.Expression is InvocationExpressionSyntax invocationExpressionSyntax)
					{
						TypeInfo info = context.SemanticModel.GetTypeInfo(invocationExpressionSyntax);

						c = context.SemanticModel.Compilation.ClassifyConversion(info.Type, expectedArgumentSymbol.Type);
					}
					else
					{
						c = context.SemanticModel.ClassifyConversion(passedArgument.Expression, expectedArgumentSymbol.Type);
					}
					if (!c.Exists)
					{
						didFindAll = false;
						break;
					}
				}

				if (didFindAll)
				{
					return true;
				}
			}
			return false;
		}
	}
}
