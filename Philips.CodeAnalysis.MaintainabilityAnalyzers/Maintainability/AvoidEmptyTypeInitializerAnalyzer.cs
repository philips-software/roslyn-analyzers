// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

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
		public override IEnumerable<Diagnostic> Analyze()
		{
			if (!Node.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				//not a static constructor
				return Option<Diagnostic>.None;
			}

			if (Node.Body == null)
			{
				//during the intellisense phase the body of a constructor can be non-existent.
				return Option<Diagnostic>.None;
			}

			if (Node.Body.Statements.Any())
			{
				//not empty
				return Option<Diagnostic>.None;
			}

			Location location = Node.GetLocation();
			return PrepareDiagnostic(location).ToSome();
		}
	}
}
