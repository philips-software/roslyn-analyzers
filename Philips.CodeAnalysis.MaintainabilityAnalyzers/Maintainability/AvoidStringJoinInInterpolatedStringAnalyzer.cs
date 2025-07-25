// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidStringJoinInInterpolatedStringAnalyzer : SingleDiagnosticAnalyzer<InterpolatedStringExpressionSyntax, AvoidStringJoinInInterpolatedStringSyntaxNodeAction>
	{
		private const string Title = @"Avoid string.Join in interpolated string";
		private const string MessageFormat = @"Consider simplifying string.Join usage in interpolated string";
		private const string Description = @"Using string.Join within interpolated strings can sometimes be simplified";

		public AvoidStringJoinInInterpolatedStringAnalyzer()
			: base(DiagnosticId.AvoidStringJoinInInterpolatedString, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}

	public class AvoidStringJoinInInterpolatedStringSyntaxNodeAction : SyntaxNodeAction<InterpolatedStringExpressionSyntax>
	{
		public override void Analyze()
		{
			// Look for interpolated string expressions
			foreach (InterpolatedStringContentSyntax content in Node.Contents)
			{
				if (content is InterpolationSyntax interpolation && ContainsStringJoinCall(interpolation.Expression))
				{
					Location location = interpolation.GetLocation();
					ReportDiagnostic(location);
				}
			}
		}

		private bool ContainsStringJoinCall(ExpressionSyntax expression)
		{
			// Check if this is a direct string.Join call
			if (expression is InvocationExpressionSyntax invocation)
			{
				// Use semantic analysis to properly identify string.Join
				SymbolInfo symbolInfo = Context.SemanticModel.GetSymbolInfo(invocation);
				if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
				{
					return methodSymbol.Name == "Join" &&
						   methodSymbol.ContainingType?.SpecialType == SpecialType.System_String;
				}
			}

			return false;
		}
	}
}
