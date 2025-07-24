// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreferInterpolatedStringAnalyzer : SingleDiagnosticAnalyzer<InvocationExpressionSyntax, PreferInterpolatedStringSyntaxNodeAction>
	{
		private const string Title = @"Prefer interpolated strings over string.Format";
		private const string MessageFormat = @"Replace string.Format with interpolated string for better readability";
		private const string Description = @"Interpolated strings are more readable and less error prone than string.Format";

		public PreferInterpolatedStringAnalyzer()
			: base(DiagnosticId.PreferInterpolatedString, Title, MessageFormat, Description, Categories.Readability)
		{ }
	}

	public class PreferInterpolatedStringSyntaxNodeAction : SyntaxNodeAction<InvocationExpressionSyntax>
	{
		public override void Analyze()
		{
			if (!IsStringFormatCall())
			{
				return;
			}

			if (!CanConvertToInterpolatedString())
			{
				return;
			}

			Location location = Node.GetLocation();
			ReportDiagnostic(location);
		}

		private bool IsStringFormatCall()
		{
			if (Node.Expression is not MemberAccessExpressionSyntax memberAccess)
			{
				return false;
			}

			if (memberAccess.Name.Identifier.Text != "Format")
			{
				return false;
			}

			if (memberAccess.Expression is not IdentifierNameSyntax identifier)
			{
				return false;
			}

			return identifier.Identifier.Text == "string";
		}

		private bool CanConvertToInterpolatedString()
		{
			if (Node.ArgumentList.Arguments.Count < 1)
			{
				return false;
			}

			ArgumentSyntax formatArgument = Node.ArgumentList.Arguments[0];
			if (formatArgument.Expression is not LiteralExpressionSyntax literal ||
				!literal.Token.IsKind(SyntaxKind.StringLiteralToken))
			{
				return false;
			}

			var formatString = literal.Token.ValueText;

			// Don't suggest conversion for format strings with format specifiers
			if (formatString.Contains(":"))
			{
				return false;
			}

			return true;
		}
	}
}
