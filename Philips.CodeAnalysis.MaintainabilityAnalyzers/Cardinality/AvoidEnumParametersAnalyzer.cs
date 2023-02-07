// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;
using static LanguageExt.Prelude;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Cardinality
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidEnumParametersAnalyzer : SingleDiagnosticAnalyzer<MethodDeclarationSyntax, AvoidEnumParametersSyntaxNodeAction>
	{

		private static readonly string Title = @"Methods should not accept enums";
		private static readonly string MessageFormat = @"Method {1} should be refactored away. Illegal Enum Parameter {0}";
		private static readonly string Description = @"Methods taking an enum as a parameter should instead live inside said enum.";

		public AvoidEnumParametersAnalyzer()
			: base(DiagnosticId.AvoidEnumParameters, Title, MessageFormat, Description, Categories.FunctionalProgramming, isEnabled: false)
		{ }
	}

	public class AvoidEnumParametersSyntaxNodeAction : SyntaxNodeAction<MethodDeclarationSyntax>
	{
		public override void Analyze()
		{
			_ = List(Node)
				.Filter((m) => !m.IsOverridden())
				.SelectMany(AnalyzeMethodParameters)
				.Iter(Context.ReportDiagnostic);
		}

		private IEnumerable<Diagnostic> AnalyzeMethodParameters(MethodDeclarationSyntax m)
		{
			return m.ParameterList.Parameters
				.Filter((p) => p.IsEnum(Context))
				.Select((p) => Diagnostic.Create(Rule, p.GetLocation(), p.Identifier.Text, m.Identifier.Text));
		}
	}

}
