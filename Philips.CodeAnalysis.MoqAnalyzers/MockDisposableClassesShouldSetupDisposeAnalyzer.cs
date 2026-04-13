// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class MockDisposableClassesShouldSetupDisposeAnalyzer : SingleDiagnosticAnalyzer
	{
		public const string PreferredDisposableMockTypeProperty = "PreferredDisposableMockType";
		private const string MockName = "Mock";
		private const string Title = @"Mock<T> of disposable concrete class should setup virtual Dispose(bool)";
		private const string MessageFormat = @"Mock<{0}> may suppress real disposal because Moq does not call base implementations by default";
		private const string Description = @"Mocking a disposable concrete class without configuring protected Dispose may prevent cleanup from executing";

		public MockDisposableClassesShouldSetupDisposeAnalyzer()
			: base(DiagnosticId.MockDisposableObjectsShouldSetupDispose, Title, MessageFormat, Description, Categories.RuntimeFailure)
		{ }

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			if (context.Compilation.GetTypeByMetadataName(StringConstants.MoqMetadata) == null)
			{
				return;
			}

			context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression, SyntaxKind.ImplicitObjectCreationExpression);
		}

		private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is not ExpressionSyntax objectCreationExpressionSyntax)
			{
				return;
			}

			if (!TryGetMockedType(context, objectCreationExpressionSyntax, out ITypeSymbol mockedType))
			{
				return;
			}

			if (mockedType is IErrorTypeSymbol)
			{
				return;
			}

			switch (mockedType.TypeKind)
			{
				case TypeKind.Interface:
				case TypeKind.Delegate:
					return;
				default:
					break;
			}

			if (!ImplementsIDisposable(mockedType, context.SemanticModel.Compilation))
			{
				return;
			}

			if (!HasVirtualDisposeBool(mockedType))
			{
				return;
			}

			if (HasDisposeSetupInSameContainingMember(context, objectCreationExpressionSyntax))
			{
				return;
			}

			ReportDiagnostic(context, objectCreationExpressionSyntax, mockedType);
		}

		private void ReportDiagnostic(SyntaxNodeAnalysisContext context, ExpressionSyntax objectCreationExpressionSyntax, ITypeSymbol mockedType)
		{
			// Pass the preferred disposable mock type from editorconfig as additional property, so that code fix can use it to determine which type to suggest in the code fix message and when applying the code fix.
			var preferredDisposableMockType = Helper.ForAdditionalFiles.GetValueFromEditorConfig(Rule.Id, "preferred_disposable_mock_type");

			ImmutableDictionary<string, string> properties = ImmutableDictionary<string, string>.Empty;
			if (!string.IsNullOrWhiteSpace(preferredDisposableMockType))
			{
				properties = properties.Add(PreferredDisposableMockTypeProperty, preferredDisposableMockType.Trim());
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreationExpressionSyntax.GetLocation(), properties, mockedType.Name));
		}

		private bool TryGetMockedType(SyntaxNodeAnalysisContext context, ExpressionSyntax objectCreationExpressionSyntax, out ITypeSymbol mockedType)
		{
			mockedType = null;

			TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(objectCreationExpressionSyntax);
			if (typeInfo.Type is not INamedTypeSymbol createdTypeSymbol || !createdTypeSymbol.IsGenericType)
			{
				return false;
			}

			if (createdTypeSymbol.Name != MockName)
			{
				return false;
			}

			if (createdTypeSymbol.ContainingNamespace == null || createdTypeSymbol.ContainingNamespace.ToDisplayString() != "Moq")
			{
				return false;
			}

			mockedType = createdTypeSymbol.TypeArguments[0];
			return true;
		}

		private static bool ImplementsIDisposable(ITypeSymbol mockedType, Compilation compilation)
		{
			INamedTypeSymbol disposableType = compilation.GetTypeByMetadataName("System.IDisposable");
			if (disposableType == null)
			{
				return false;
			}

			if (SymbolEqualityComparer.Default.Equals(mockedType, disposableType))
			{
				return true;
			}

			return mockedType.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, disposableType));
		}

		private bool HasVirtualDisposeBool(ITypeSymbol mockedType)
		{
			for (ITypeSymbol current = mockedType; current != null; current = current.BaseType)
			{
				foreach (IMethodSymbol method in current.GetMembers(nameof(System.IDisposable.Dispose)).OfType<IMethodSymbol>())
				{
					if (IsVirtualDisposeBool(method))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool IsVirtualDisposeBool(IMethodSymbol method)
		{
			if (method.MethodKind != MethodKind.Ordinary)
			{
				return false;
			}

			if (method.Parameters.Length != 1)
			{
				return false;
			}

			if (method.Parameters[0].Type == null || method.Parameters[0].Type.SpecialType != SpecialType.System_Boolean)
			{
				return false;
			}

			return method.DeclaredAccessibility switch
			{
				Accessibility.Protected or
				Accessibility.ProtectedOrInternal or
				Accessibility.ProtectedAndInternal => method.IsVirtual || method.IsOverride,
				_ => false,
			};
		}

		private bool HasDisposeSetupInSameContainingMember(SyntaxNodeAnalysisContext context, ExpressionSyntax objectCreationExpressionSyntax)
		{
			ISymbol mockSymbol = GetAssignedMockSymbol(context, objectCreationExpressionSyntax);
			if (mockSymbol == null)
			{
				return false;
			}

			SyntaxNode containingMember = GetContainingMember(objectCreationExpressionSyntax);
			if (containingMember == null)
			{
				return false;
			}

			foreach (InvocationExpressionSyntax invocationExpressionSyntax in containingMember.DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				if (IsDisposeSetupInvocation(context, invocationExpressionSyntax, mockSymbol))
				{
					return true;
				}
			}

			return false;
		}

		private static SyntaxNode GetContainingMember(SyntaxNode node)
		{
			SyntaxNode containingMember = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
			if (containingMember != null)
			{
				return containingMember;
			}

			containingMember = node.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
			if (containingMember != null)
			{
				return containingMember;
			}

			containingMember = node.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
			if (containingMember != null)
			{
				return containingMember;
			}

			containingMember = node.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();
			return containingMember;
		}

		private ISymbol GetAssignedMockSymbol(SyntaxNodeAnalysisContext context, ExpressionSyntax objectCreationExpressionSyntax)
		{
			// Local variable, including "using var mock = ..."
			var equalsValueClauseSyntax = objectCreationExpressionSyntax.Parent as EqualsValueClauseSyntax;
			if (equalsValueClauseSyntax?.Parent is VariableDeclaratorSyntax variableDeclaratorSyntax)
			{
				return context.SemanticModel.GetDeclaredSymbol(variableDeclaratorSyntax);
			}

			// Assignment to a field/property/local: mock = new Mock<T>();
			if (objectCreationExpressionSyntax.Parent is AssignmentExpressionSyntax assignmentExpressionSyntax)
			{
				SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(assignmentExpressionSyntax.Left);
				return symbolInfo.Symbol;
			}

			return null;
		}

		private bool IsDisposeSetupInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, ISymbol expectedMockSymbol)
		{
			// Looking for:
			// mock.Protected().Setup(...Dispose...)
			// We intentionally do not require CallBase() in v1.

			if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
			{
				return false;
			}

			if (memberAccessExpressionSyntax.Name.Identifier.ValueText != "Setup")
			{
				return false;
			}

			if (!IsDisposeSetupArgumentList(invocationExpressionSyntax.ArgumentList))
			{
				return false;
			}

			if (memberAccessExpressionSyntax.Expression is not InvocationExpressionSyntax protectedInvocationExpressionSyntax)
			{
				return false;
			}

			if (protectedInvocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax protectedMemberAccessExpressionSyntax)
			{
				return false;
			}

			if (protectedMemberAccessExpressionSyntax.Name.Identifier.ValueText != "Protected")
			{
				return false;
			}

			SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(protectedMemberAccessExpressionSyntax.Expression);
			if (symbolInfo.Symbol == null)
			{
				return false;
			}

			return SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol, expectedMockSymbol);
		}

		private static bool IsDisposeSetupArgumentList(ArgumentListSyntax argumentListSyntax)
		{
			if (argumentListSyntax == null || argumentListSyntax.Arguments.Count == 0)
			{
				return false;
			}

			foreach (ArgumentSyntax argumentSyntax in argumentListSyntax.Arguments)
			{
				if (ArgumentLooksLikeDispose(argumentSyntax.Expression))
				{
					return true;
				}
			}

			return false;
		}

		private static bool ArgumentLooksLikeDispose(ExpressionSyntax expressionSyntax)
		{
			if (expressionSyntax is LiteralExpressionSyntax literalExpressionSyntax && literalExpressionSyntax.IsKind(SyntaxKind.StringLiteralExpression))
			{
				return literalExpressionSyntax.Token.ValueText == nameof(System.IDisposable.Dispose);
			}

			if (expressionSyntax is InvocationExpressionSyntax invocationExpressionSyntax &&
				invocationExpressionSyntax.Expression is IdentifierNameSyntax identifierNameSyntax &&
				identifierNameSyntax.Identifier.ValueText == "nameof" &&
				invocationExpressionSyntax.ArgumentList != null &&
				invocationExpressionSyntax.ArgumentList.Arguments.Count == 1)
			{
				var argumentText = invocationExpressionSyntax.ArgumentList.Arguments[0].ToString();
				return argumentText == nameof(System.IDisposable.Dispose) ||
					argumentText.EndsWith("." + nameof(System.IDisposable.Dispose));
			}

			return false;
		}
	}
}
