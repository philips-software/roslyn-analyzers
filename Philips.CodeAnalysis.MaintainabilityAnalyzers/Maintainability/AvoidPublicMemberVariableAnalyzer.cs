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
	public class AvoidPublicMemberVariableAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid public fields declaration";
		public const string MessageFormat = @"Avoid public instance fields in a class. Use property instead";
		private const string Description = @"Avoid public  fields in a class. Declare public property if needed for static fields";
		private const string Category = Categories.Maintainability;

		public DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidPublicMemberVariables), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.FieldDeclaration);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			FieldDeclarationSyntax fieldDeclaration = (FieldDeclarationSyntax)context.Node;

			// ignore struct
			if (fieldDeclaration.Parent.Kind() == SyntaxKind.StructDeclaration)
			{
				return;
			}

			if (fieldDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
			{
				// ignore the const
				if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
				{
					return;
				}

				// ignore the static
				if (fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
				{
					return;
				}

				Diagnostic diagnostic = Diagnostic.Create(Rule, fieldDeclaration.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}