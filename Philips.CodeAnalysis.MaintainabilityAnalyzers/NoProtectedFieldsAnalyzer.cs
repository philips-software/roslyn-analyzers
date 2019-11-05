// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	/// <summary>
	/// Don't allow protected fields, they violate encapsulation
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoProtectedFieldsAnalyzer : DiagnosticAnalyzer
	{
		#region Non-Public Data Members

		private const string Title = @"Do not use protected fields";
		private const string MessageFormat = @"Do not use protected fields";
		private const string Description = @"";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.NoProtectedFields), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		#endregion

		#region Non-Public Properties/Methods

		#endregion

		#region Public Interface

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(OnField, SyntaxKind.FieldDeclaration);
		}

		private void OnField(SyntaxNodeAnalysisContext context)
		{
			FieldDeclarationSyntax fieldDeclarationSyntax = (FieldDeclarationSyntax)context.Node;

			if (fieldDeclarationSyntax.Modifiers.Any(SyntaxKind.ProtectedKeyword))
			{
				context.ReportDiagnostic(Diagnostic.Create(_rule, fieldDeclarationSyntax.GetLocation()));
			}
		}

		#endregion
	}
}
