using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;



namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoRegionsInMethodAnalyzer : DiagnosticAnalyzer

	{
		public const string DiagnosticId = "NoRegionsInMethods";
		private static readonly string Title = "No Regions In Methods";
		private static readonly string MessageFormat = "Regions are not allowed to start or end within a method";
		private static readonly string Description = "A region can not start or end within a method, instead long methods should be refactored for length and clarity";
		private const string Category = "Maintainability";

		public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);


			context.RegisterSyntaxNodeAction(OnMethod, SyntaxKind.MethodDeclaration);
		}

		private static void OnMethod(SyntaxNodeAnalysisContext context)
		{
	
			BaseMethodDeclarationSyntax node = (BaseMethodDeclarationSyntax)context.Node;

			var methodBody = node.Body.ToString().ToLower();

			if (methodBody.Contains("#region") || methodBody.Contains("#endregion"))
			{
				var diagnostic = Diagnostic.Create(Rule, node.GetLocation());
				System.Console.WriteLine(node.GetLocation().ToString());
				context.ReportDiagnostic(diagnostic);
			}

		}
	}
}
