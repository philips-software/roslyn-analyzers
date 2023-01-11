﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReduceCognitiveLoadAnalyzer : DiagnosticAnalyzer
	{
		public ReduceCognitiveLoadAnalyzer()
		{ }

		public ReduceCognitiveLoadAnalyzer(int maxCognitiveLoad)
		{
			MaxCognitiveLoad = maxCognitiveLoad;
		}

		private int MaxCognitiveLoad { get; set; }

		private const string Title = @"Reduce Cognitive Load";
		public const string MessageFormat = @"Reduce Cognitive Load of {0} to threshold of {1}";
		private const string Description = @"This method has too many nested block statements and logical cases to consider in this method";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.ReduceCognitiveLoad), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
		}

		private int CalcCognitiveLoad(BlockSyntax blockSyntax)
		{
			int cognitiveLoad = 1;
			foreach (BlockSyntax descBlockSyntax in blockSyntax.DescendantNodes().OfType<BlockSyntax>())
			{
				cognitiveLoad += CalcCognitiveLoad(descBlockSyntax);
			}
			return cognitiveLoad;
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
			BlockSyntax blockSyntax = methodDeclarationSyntax.DescendantNodes().OfType<BlockSyntax>().First();
			int cognitiveLoad = CalcCognitiveLoad(blockSyntax);

			cognitiveLoad += blockSyntax.DescendantTokens().Where((token) =>
			{
				return
					token.IsKind(SyntaxKind.BarBarToken) ||
					token.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
					token.IsKind(SyntaxKind.ExclamationToken) ||
					token.IsKind(SyntaxKind.ExclamationEqualsToken) ||
					token.IsKind(SyntaxKind.ReturnKeyword) ||
					token.IsKind(SyntaxKind.BreakKeyword) ||
					token.IsKind(SyntaxKind.ContinueKeyword)
					;
			}).Count();

			InitializeMaxCognitiveLoad(context);
			if (cognitiveLoad > MaxCognitiveLoad)
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, methodDeclarationSyntax.GetLocation(), cognitiveLoad, MaxCognitiveLoad);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private void InitializeMaxCognitiveLoad(SyntaxNodeAnalysisContext context)
		{
			if (MaxCognitiveLoad == 0)
			{
				var additionalFilesHelper = new AdditionalFilesHelper(context.Options, context.Compilation);
				string configuredMaxCognitiveLoad = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"max_cognitive_load");
				if (int.TryParse(configuredMaxCognitiveLoad, out int maxAllowedCognitiveLoad))
				{
					if (maxAllowedCognitiveLoad is >= 5 and <= 100)
					{
						MaxCognitiveLoad = maxAllowedCognitiveLoad;
						return;
					}
				}
				MaxCognitiveLoad = 25;
			}
		}
	}
}
