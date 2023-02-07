// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidEmptyTypeInitializerAnalyzer : SingleDiagnosticAnalyzer<ConstructorDeclarationSyntax, AvoidEmptyTypeInitializerSyntaxNodeAction>
	{
		private const string Title = @"Avoid Empty Type Initializer";
		public const string MessageFormat = @"Remove empty type initializer";
		private const string Description = MessageFormat;

		public AvoidEmptyTypeInitializerAnalyzer()
			: base(DiagnosticId.AvoidEmptyTypeInitializer, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class AvoidEmptyTypeInitializerSyntaxNodeAction : SyntaxNodeAction<ConstructorDeclarationSyntax>
	{
		public override void Analyze()
		{
			if (!Node.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				//not a static constructor
				return;
			}

			if (Node.Body == null)
			{
				//during the intellisense phase the body of a constructor can be non-existent.
				return;
			}

			if (Node.Body.Statements.Any())
			{
				//not empty
				return;
			}

			ReportDiagnostic(Node.GetLocation());
		}
	}
}
