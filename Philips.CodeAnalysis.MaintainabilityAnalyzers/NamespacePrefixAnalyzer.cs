using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NamespacePrefixAnalyzer : DiagnosticAnalyzer
	{
		#region Non-Public Data Members

		private const string Title = @"Namespace must use the predefined prefixes";
		private const string MessageFormat = @"Namespace must use the predefined prefixes. ie. [OrganizationName].[ProductName]";
		private const string Description = @"Namespace must use the predefined prefixes";
		private const string Category = Categories.Naming;

		#endregion

		#region Non-Public Properties/Methods
		private void Analyze(SyntaxNodeAnalysisContext context)
		{

			AdditionalFilesHelper additionalFilesHelper = new AdditionalFilesHelper(context.Options, context.Compilation);
			string expected_prefix = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"namespace_prefix");

			NamespaceDeclarationSyntax namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
			string myNamespace = namespaceDeclaration.Name.ToString();
			if (!myNamespace.StartsWith(expected_prefix))
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, namespaceDeclaration.Name.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}



		#endregion


		#region Public Interface

		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.NamespacePrefix), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }


		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NamespaceDeclaration);
			//context.RegisterCompilationStartAction(compilationContext =>
			//{
			//	compilationContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NamespaceDeclaration);
			//});

		}

		#endregion
	}
}
