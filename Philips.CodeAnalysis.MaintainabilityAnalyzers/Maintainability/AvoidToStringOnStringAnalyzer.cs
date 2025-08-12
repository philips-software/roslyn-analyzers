// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidToStringOnStringAnalyzer : SingleDiagnosticAnalyzer<MemberAccessExpressionSyntax, AvoidToStringOnStringSyntaxNodeAction>
	{
		private const string Title = @"Avoid calling ToString() on string type";
		private const string MessageFormat = @"Avoid calling ToString() on string type. It is redundant.";
		private const string Description = @"Calling ToString() on an expression that is already of type string is redundant and should be removed.";

		public AvoidToStringOnStringAnalyzer()
			: base(DiagnosticId.AvoidToStringOnString, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: true)
		{ }
	}

	public class AvoidToStringOnStringSyntaxNodeAction : SyntaxNodeAction<MemberAccessExpressionSyntax>
	{
		public override void Analyze()
		{
			// Check if this is a ToString() method call
			if (Node.Name is not IdentifierNameSyntax { Identifier.ValueText: StringConstants.ToStringMethodName })
			{
				return;
			}

			// Get the parent to see if this is actually a method invocation
			if (Node.Parent is not InvocationExpressionSyntax invocation)
			{
				return;
			}

			// Check if ToString() is called with no arguments
			if (invocation.ArgumentList.Arguments.Count > 0)
			{
				return;
			}

			// Get the semantic model to check the type of the expression
			SemanticModel semanticModel = Context.SemanticModel;

			// Get the type of the expression being accessed
			TypeInfo typeInfo = semanticModel.GetTypeInfo(Node.Expression);
			ITypeSymbol expressionType = typeInfo.Type;

			// Check if the expression type is string
			if (expressionType?.SpecialType == SpecialType.System_String)
			{
				Location location = Node.Name.GetLocation();
				ReportDiagnostic(location);
			}
		}
	}
}