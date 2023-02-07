// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NamespacePrefixAnalyzer : DiagnosticAnalyzer
	{
		private const string TitleForIncorrectPrefix = @"Namespace must use  predefined prefixes";
		private const string MessageFormatForIncorrectPrefix = @"Namespace must use the predefined prefixes configured in the .editorconfig file";
		private const string DescriptionForIncorrectPrefix = MessageFormatForIncorrectPrefix;


		private const string TitleForEmptyPrefix = @"Specify the namespace prefix in the .editorconfig file";
		private const string MessageFormatForEmptyPrefix = @"Please specify the namespace prefix in the .editorconfig file Eg. dotnet_code_quality.{0}.namespace_prefix = [OrganizationName].[ProductName]";
		private const string DescriptionForEmptyPrefix = MessageFormatForEmptyPrefix;
		private const string Category = Categories.Naming;

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			AdditionalFilesHelper additionalFilesHelper = new(context.Options, context.Compilation);
			string expectedPrefix = additionalFilesHelper.GetValueFromEditorConfig(RuleForIncorrectNamespace.Id, @"namespace_prefix");

			NamespaceDeclarationSyntax namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
			string myNamespace = namespaceDeclaration.Name.ToString();
			var location = namespaceDeclaration.Name.GetLocation();
			if (string.IsNullOrEmpty(expectedPrefix))
			{
				Diagnostic diagnostic = Diagnostic.Create(RuleForEmptyPrefix, location);
				context.ReportDiagnostic(diagnostic);
			}
			else if (!myNamespace.StartsWith(expectedPrefix))
			{
				if (Helper.IsNamespaceExempt(myNamespace))
				{
					return;
				}

				Diagnostic diagnostic = Diagnostic.Create(RuleForIncorrectNamespace, location);
				context.ReportDiagnostic(diagnostic);
			}
		}

		public static readonly string RuleId = Helper.ToDiagnosticId(DiagnosticId.NamespacePrefix);

		public static readonly DiagnosticDescriptor RuleForIncorrectNamespace = new(RuleId, TitleForIncorrectPrefix, MessageFormatForIncorrectPrefix, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DescriptionForIncorrectPrefix);
		public static readonly DiagnosticDescriptor RuleForEmptyPrefix = new(RuleId, TitleForEmptyPrefix, string.Format(MessageFormatForEmptyPrefix, RuleId), Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: string.Format(DescriptionForEmptyPrefix, RuleId));


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleForIncorrectNamespace, RuleForEmptyPrefix); } }


		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NamespaceDeclaration);
		}
	}
}
