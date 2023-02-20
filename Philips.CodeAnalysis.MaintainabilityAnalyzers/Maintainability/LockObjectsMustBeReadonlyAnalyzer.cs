// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LockObjectsMustBeReadonlyAnalyzer : SingleDiagnosticAnalyzer<LockStatementSyntax, LockObjectsMustBeReadonlySyntaxNodeAction>
	{
		private const string Title = @"Objects used as locks should be readonly";
		private const string MessageFormat = @"'{0}' should be readonly";
		private const string Description = @"";

		public LockObjectsMustBeReadonlyAnalyzer()
			: base(DiagnosticId.LocksShouldBeReadonly, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class LockObjectsMustBeReadonlySyntaxNodeAction : SyntaxNodeAction<LockStatementSyntax>
	{
		public override void Analyze()
		{
			if (Node.Expression is IdentifierNameSyntax identifier)
			{
				SymbolInfo info = Context.SemanticModel.GetSymbolInfo(identifier);

				if (info.Symbol is IFieldSymbol field && !field.IsReadOnly)
				{
					var location = identifier.GetLocation();
					ReportDiagnostic(location, identifier.ToString());
				}
			}
		}
	}
}
