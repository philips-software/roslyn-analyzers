// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAsyncVoidAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid async void";
		public const string MessageFormat = @"Methods may not have async void return type";
		private const string Description = @"To avoid unhandled exception, methods should use async void unless a event handler.";
		private const string Category = Categories.Maintainability;
		private const string HelpUri = @"https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming#avoid-async-void";

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidTaskVoid), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description, helpLinkUri: HelpUri);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public string Identifier = @"void";

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task") == null)
				{
					return;
				}
				startContext.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.MethodDeclaration);
			});
		}

		private void AnalyzeIfStatement(SyntaxNodeAnalysisContext obj)
		{
			SyntaxTree tree = obj.Node.SyntaxTree;
			CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
			IEnumerable<MethodDeclarationSyntax> allMethodDeclaration = from methodDeclaration in root.DescendantNodes()
										.OfType<MethodDeclarationSyntax>()
																		select methodDeclaration;

			var allMethodDeclarationList = allMethodDeclaration.ToList();

			allMethodDeclarationList.ForEach(x =>
			{
				if (x.Modifiers.Any(z => z.Kind() == SyntaxKind.AsyncKeyword) && x.ReturnType.ToString() == Identifier)
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, x.GetLocation());
					obj.ReportDiagnostic(diagnostic);
				}
			});
		}
	}
}
