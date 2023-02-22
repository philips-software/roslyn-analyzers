﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;
using static LanguageExt.Prelude;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Cardinality
{

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidVoidReturnAnalyzer : SingleDiagnosticAnalyzer<MethodDeclarationSyntax, AvoidVoidReturnSyntaxNodeAction>
	{
		private const string Title = @"Method returns void";
		private const string MessageFormat = @"Method '{0}' returns void";
		private const string Description = @"Void returns imply a hidden side effect, since there is otherwise a singularly unique unit function.";

		public AvoidVoidReturnAnalyzer()
			: base(DiagnosticId.AvoidVoidReturn, Title, MessageFormat, Description, Categories.FunctionalProgramming, isEnabled: false)
		{ }
	}

	public class AvoidVoidReturnSyntaxNodeAction : SyntaxNodeAction<MethodDeclarationSyntax>
	{
		public override void Analyze()
		{
			_ = Optional(Node)
				.Filter((m) => m.ReturnsVoid())
				.Filter((m) => !m.IsOverridden())
				.Select((m) => m.CreateDiagnostic(Rule))
				.Iter(Context.ReportDiagnostic);
		}
	}
}
