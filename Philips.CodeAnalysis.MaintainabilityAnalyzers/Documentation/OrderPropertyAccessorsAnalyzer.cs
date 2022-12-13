// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class OrderPropertyAccessorsAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Accessors should be ordered";
		private const string MessageFormat = @"Accessors should be ordered.";
		private const string Description = @"Properties should be ordered get then set (or init)";
		private const string Category = Categories.Documentation;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.OrderPropertyAccessors), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.PropertyDeclaration);
			});
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			PropertyDeclarationSyntax node = (PropertyDeclarationSyntax)context.Node;

			AccessorListSyntax accessors = node.AccessorList;

			if (accessors is null)
			{
				return;
			}

			int getIndex = -1;
			int setIndex = int.MaxValue;

			for (int i = 0; i < accessors.Accessors.Count; i++)
			{
				var accessor = accessors.Accessors[i];

				if (accessor.Keyword.IsKind(SyntaxKind.GetKeyword))
				{
					getIndex = i;
					continue;
				}

				// SyntaxKind.InitKeyword doesn't exist in the currently used version of Roslyn (it exists in at least 3.9.0)
				if (accessor.Keyword.IsKind(SyntaxKind.SetKeyword) || accessor.Keyword.Text == "init")
				{
					setIndex = i;
					continue;
				}
			}

			if (setIndex < getIndex)
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, accessors.GetLocation()));
			}


			//get, set
		}
	}
}
