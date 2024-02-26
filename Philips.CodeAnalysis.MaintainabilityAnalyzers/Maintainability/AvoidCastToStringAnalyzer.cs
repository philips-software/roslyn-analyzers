// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidCastToStringAnalyzer : SingleDiagnosticAnalyzer<ConversionOperatorDeclarationSyntax, AvoidCastToStringSyntaxNodeAction>
	{
		private const string Title = @"Avoid casting to string";
		private const string MessageFormat = @"Avoid casting to string, use `ToString()` or a serialization solution.";
		private const string Description = @"Avoid casting to string, use `ToString()` or a serialization solution";

		public AvoidCastToStringAnalyzer()
			: base(DiagnosticId.AvoidCastToString, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}

	public class AvoidCastToStringSyntaxNodeAction : SyntaxNodeAction<ConversionOperatorDeclarationSyntax>
	{
		public override void Analyze()
		{
			if (Node.Type is not PredefinedTypeSyntax { Keyword.Text: "string" })
			{
				return;
			}

			Location loc = Node.OperatorKeyword.GetLocation();
			ReportDiagnostic(loc);
		}
	}
}
