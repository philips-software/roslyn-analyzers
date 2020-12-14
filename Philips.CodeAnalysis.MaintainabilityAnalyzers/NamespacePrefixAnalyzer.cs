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

		private const string TitleForIncorrectPrefix = @"Namespace must use  predefined prefixes";
		private const string MessageFormatForIncorrectPrefix = @"Namespace must use the predefined prefixes configured in the .editorconfig file";
		private const string DescriptionForIncorrectPrefix = @"Namespace must use the predefined prefixes configured in the .editorconfig file";


		private const string TitleForEmptyPrefix = @"Specify the namespace prefix in the .editorconfig file";
		private const string MessageFormatForEmptyPrefix = @"Please specify the namespace prefix in the .editorconfig file Eg. dotnet_code_quality.{0}.namespace_prefix = [OrganizationName].[ProductName]";
		private const string DescriptionForEmptyPrefix = @"Please specify the namespace prefix in the .editorconfig file Eg. dotnet_code_quality.{0}.namespace_prefix = [OrganizationName].[ProductName]";
		private const string Category = Categories.Naming;
		#endregion

		#region Non-Public Properties/Methods
		private void Analyze(SyntaxNodeAnalysisContext context)
		{

			AdditionalFilesHelper additionalFilesHelper = new AdditionalFilesHelper(context.Options, context.Compilation);
			string expected_prefix = additionalFilesHelper.GetValueFromEditorConfig(RuleForIncorrectNamespace.Id, @"namespace_prefix");

			NamespaceDeclarationSyntax namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
			string myNamespace = namespaceDeclaration.Name.ToString();
			if (string.IsNullOrEmpty(expected_prefix))
			{
				Diagnostic diagnostic = Diagnostic.Create(RuleForEmptyPrefix, namespaceDeclaration.Name.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
			else if (!myNamespace.StartsWith(expected_prefix))
			{
				Diagnostic diagnostic = Diagnostic.Create(RuleForIncorrectNamespace, namespaceDeclaration.Name.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}

		#endregion


		#region Public Interface
		public static readonly string RuleId = Helper.ToDiagnosticId(DiagnosticIds.NamespacePrefix);

		public static DiagnosticDescriptor RuleForIncorrectNamespace = new DiagnosticDescriptor(RuleId, TitleForIncorrectPrefix, MessageFormatForIncorrectPrefix, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DescriptionForIncorrectPrefix);
		public static DiagnosticDescriptor RuleForEmptyPrefix = new DiagnosticDescriptor(RuleId, TitleForEmptyPrefix, string.Format(MessageFormatForEmptyPrefix, RuleId), Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: string.Format(DescriptionForEmptyPrefix, RuleId));


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleForIncorrectNamespace, RuleForEmptyPrefix); } }


		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NamespaceDeclaration);
		}

		#endregion
	}
}
