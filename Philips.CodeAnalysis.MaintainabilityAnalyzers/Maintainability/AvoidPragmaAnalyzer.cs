// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidPragmaAnalyzer : SingleDiagnosticAnalyzer<PragmaWarningDirectiveTriviaSyntax, AvoidPragmaSyntaxNodeAction>
	{
		private const string Title = @"Avoid Pragma Warning";
		public const string MessageFormat = @"Do not use #pragma warning";
		private const string Description = MessageFormat;

		public AvoidPragmaAnalyzer()
			: base(DiagnosticId.AvoidPragma, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class AvoidPragmaSyntaxNodeAction : SyntaxNodeAction<PragmaWarningDirectiveTriviaSyntax>
	{
		public override void Analyze()
		{
			var myOwnId = DiagnosticId.AvoidPragma.ToId();
			if (Node.ErrorCodes.Where(e => e.IsKind(SyntaxKind.IdentifierName))
									.Any(i => i.ToString().Contains(myOwnId)))
			{
				return;
			}

			CSharpSyntaxNode violation = Node;
			Location location = violation.GetLocation();
			ReportDiagnostic(location);
		}
	}
}
