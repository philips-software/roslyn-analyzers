// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoEmptyConstructorAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"No Empty Constructors";
		public const string MessageFormat = @"Remove empty constructor";
		private const string Description = @"Remove empty constructor";
		private const string Category = Categories.Maintainability;

		public DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.RemoveEmptyConstructors), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ConstructorDeclaration);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			ConstructorDeclarationSyntax ctor = (ConstructorDeclarationSyntax)context.Node;

			if (!ctor.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				//not a static constructor
				return;
			}

			if (ctor.Body == null)
			{
				//during the intellisense phase the body of a constructor can be non-existent.
				return;
			}

			if (ctor.Body.Statements.Any())
			{
				//not empty
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, ctor.GetLocation()));
		}
	}
}
