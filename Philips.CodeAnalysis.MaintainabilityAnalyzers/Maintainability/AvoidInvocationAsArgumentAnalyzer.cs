// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidInvocationAsArgumentAnalyzer : SingleDiagnosticAnalyzer<ArgumentSyntax, AvoidInvocationAsArgumentSyntaxNodeAction>
	{
		private const string Title = @"Avoid method calls as arguments";
		public const string MessageFormat = @"Avoid '{0}' as an argument";
		private const string Description = @"Avoid method calls as arguments to method calls";
		public AvoidInvocationAsArgumentAnalyzer()
			: base(DiagnosticId.AvoidInvocationAsArgument, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class AvoidInvocationAsArgumentSyntaxNodeAction : SyntaxNodeAction<ArgumentSyntax>
	{
		public override void Analyze()
		{
			// We are looking for method calls as arguments
			if (Node.Expression is not InvocationExpressionSyntax argumentExpressionSyntax)
			{
				return;
			}

			// If it's an embedded nameof() operation, let it go.
			if ((argumentExpressionSyntax.Expression as IdentifierNameSyntax)?.Identifier.Text == "nameof")
			{
				return;
			}

			// If it's calling ToString(), let it go. (ToStrings() cognitive load isn't excessive, and lots of violations)
			string methodName = (argumentExpressionSyntax.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text;
			if (methodName is StringConstants.ToStringMethodName or StringConstants.ToArrayMethodName or StringConstants.ToListMethodName)
			{
				return;
			}

			// If nested calls (e.g., Foo(Bar(Meow()))), only trigger the outer violation Bar(Meow())
			if (Node.Ancestors().OfType<ArgumentSyntax>().Any())
			{
				return;
			}

			// If we're within a constructor initializer (this(...) or base(...) eg), let it go
			ConstructorInitializerSyntax constructorInitializerSyntax = Node.Ancestors().OfType<ConstructorInitializerSyntax>().FirstOrDefault();
			if (constructorInitializerSyntax != null)
			{
				return;
			}

			// If the caller is Assert, let it go. (This is debatable, and ideally warrants a configuration option.)
			MemberAccessExpressionSyntax caller = (Node.Parent.Parent as InvocationExpressionSyntax)?.Expression as MemberAccessExpressionSyntax;
			if (caller?.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText.Contains(@"Assert"))
			{
				return;
			}

			// If the called method is static, let it go to reduce annoyances. E.g., "Times.Once", "Mock.Of<>", "Marshal.Sizeof", etc.
			// This is debatable, and ideally warrants a configuration option.
			if (argumentExpressionSyntax.Expression is MemberAccessExpressionSyntax callee)
			{
				var symbol = Context.SemanticModel.GetSymbolInfo(callee).Symbol;
				if (symbol.IsStatic)
				{
					return;
				}
			}

			var location = Node.GetLocation();
			ReportDiagnostic(location, Node.ToString());
		}
	}
}
