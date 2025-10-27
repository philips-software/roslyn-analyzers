// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidStringFormatInInterpolatedStringAnalyzer : SingleDiagnosticAnalyzer<InterpolatedStringExpressionSyntax,
		AvoidStringFormatInInterpolatedStringSyntaxNodeAction>
	{
		private const string Title = @"Avoid string.Format in interpolated string";
		private const string MessageFormat = @"Consider simplifying string.Format usage in interpolated string";
		private const string Description = @"Using string.Format within interpolated strings can be simplified by using direct interpolation";

		public AvoidStringFormatInInterpolatedStringAnalyzer()
			: base(DiagnosticId.AvoidStringFormatInInterpolatedString, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }

		protected override SyntaxKind GetSyntaxKind()
		{
			return SyntaxKind.InterpolatedStringExpression;
		}
	}

	public class AvoidStringFormatInInterpolatedStringSyntaxNodeAction : SyntaxNodeAction<InterpolatedStringExpressionSyntax>
	{
		public override void Analyze()
		{
			// Look for interpolated string expressions
			foreach (InterpolatedStringContentSyntax content in Node.Contents)
			{
				if (content is InterpolationSyntax interpolation && ContainsStringFormatCall(interpolation.Expression))
				{
					Location location = interpolation.GetLocation();
					ReportDiagnostic(location);
				}
			}
		}

		private bool ContainsStringFormatCall(ExpressionSyntax expression)
		{
			// Check if this is a direct string.Format call
			if (expression is InvocationExpressionSyntax invocation)
			{
				// Use semantic analysis to properly identify string.Format
				SymbolInfo symbolInfo = Context.SemanticModel.GetSymbolInfo(invocation);
				if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
				{
					return methodSymbol.Name == "Format" &&
						   methodSymbol.ContainingType?.SpecialType == SpecialType.System_String;
				}
			}

			return false;
		}
	}
}
