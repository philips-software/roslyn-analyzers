// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidInvocationAsArgumentAnalyzer : SingleDiagnosticAnalyzer<ArgumentSyntax, AvoidInvocationAsArgumentSyntaxNodeAction>
	{
		private const string Title = @"Avoid method calls as arguments";
		public const string MessageFormat = @"Avoid '{0}' as an argument";
		private const string Description = @"Avoid method calls as arguments to method calls";
		public AvoidInvocationAsArgumentAnalyzer()
			: base(DiagnosticId.AvoidInvocationAsArgument, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}

	public class AvoidInvocationAsArgumentSyntaxNodeAction : SyntaxNodeAction<ArgumentSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			// We are looking for method calls as arguments
			if (Node.Expression is not InvocationExpressionSyntax invocationExpressionSyntax)
			{
				return Option<Diagnostic>.None;
			}

			// If it's an embedded nameof() operation, let it go.
			if ((invocationExpressionSyntax.Expression as IdentifierNameSyntax)?.Identifier.Text == "nameof")
			{
				return Option<Diagnostic>.None;
			}

			// If it's calling ToString(), let it go. (ToStrings() cognitive load isn't excessive, and lots of violations)
			var methodName = (invocationExpressionSyntax.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text;
			if (methodName is StringConstants.ToStringMethodName or StringConstants.ToArrayMethodName or StringConstants.ToListMethodName)
			{
				return Option<Diagnostic>.None;
			}

			// If nested calls (e.g., Foo(Bar(Meow()))), only trigger the outer violation Bar(Meow())
			if (Node.Ancestors().OfType<ArgumentSyntax>().Any(arg => !IsStaticMethod(arg.Expression)))
			{
				return Option<Diagnostic>.None;
			}

			// If we're within a constructor initializer (this(...) or base(...) eg), let it go
			ConstructorInitializerSyntax constructorInitializerSyntax = Node.Ancestors().OfType<ConstructorInitializerSyntax>().FirstOrDefault();
			if (constructorInitializerSyntax != null)
			{
				return Option<Diagnostic>.None;
			}

			// If the caller is Assert, let it go. (This is debatable, and ideally warrants a configuration option.)
			var caller = (Node.Parent.Parent as InvocationExpressionSyntax)?.Expression as MemberAccessExpressionSyntax;
			if (caller?.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText.Contains(@"Assert"))
			{
				return Option<Diagnostic>.None;
			}

			// If the called method is static, let it go to reduce annoyances. E.g., "Times.Once", "Mock.Of<>", "Marshal.Sizeof", etc.
			// This is debatable, and ideally warrants a configuration option.
			if (invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax callee)
			{
				var isStatic = IsStaticMethod(callee);
				if (isStatic)
				{
					return Option<Diagnostic>.None;
				}
			}

			Location location = Node.GetLocation();
			return PrepareDiagnostic(location, Node.ToString()).ToSome();
		}

		private bool IsStaticMethod(SyntaxNode node)
		{
			ISymbol symbol = Context.SemanticModel.GetSymbolInfo(node).Symbol;
			return symbol is { IsStatic: true };
		}
	}
}
