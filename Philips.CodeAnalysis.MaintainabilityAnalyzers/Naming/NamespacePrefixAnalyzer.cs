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
	public class NamespacePrefixAnalyzer : DiagnosticAnalyzerBase
	{
		private const string TitleForIncorrectPrefix = @"Namespace uses predefined prefix";
		private const string MessageFormatForIncorrectPrefix = @"Namespace must use the predefined prefixes configured in the .editorconfig file";
		private const string DescriptionForIncorrectPrefix = MessageFormatForIncorrectPrefix;


		private const string TitleForEmptyPrefix = @"Specify the namespace prefix in the .editorconfig file";
		private const string Category = Categories.Naming;

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var expectedPrefix = Helper.ForAdditionalFiles.GetValueFromEditorConfig(RuleForIncorrectNamespace.Id, @"namespace_prefix");

			var namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
			var myNamespace = namespaceDeclaration.Name.ToString();
			Location location = namespaceDeclaration.Name.GetLocation();
			if (string.IsNullOrEmpty(expectedPrefix))
			{
				var diagnostic = Diagnostic.Create(RuleForEmptyPrefix, location);
				context.ReportDiagnostic(diagnostic);
			}
			else if (!myNamespace.StartsWith(expectedPrefix))
			{
				if (Helper.ForNamespaces.IsNamespaceExempt(myNamespace))
				{
					return;
				}

				var diagnostic = Diagnostic.Create(RuleForIncorrectNamespace, location);
				context.ReportDiagnostic(diagnostic);
			}
		}

		public static readonly string RuleId = DiagnosticId.NamespacePrefix.ToId();

		public static readonly DiagnosticDescriptor RuleForIncorrectNamespace = new(RuleId, TitleForIncorrectPrefix, MessageFormatForIncorrectPrefix, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DescriptionForIncorrectPrefix);
		public static readonly DiagnosticDescriptor RuleForEmptyPrefix = new(RuleId, TitleForEmptyPrefix, $"Please specify the namespace prefix in the .editorconfig file Eg. dotnet_code_quality.{RuleId}.namespace_prefix = [OrganizationName].[ProductName]", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: $"Please specify the namespace prefix in the .editorconfig file Eg. dotnet_code_quality.{RuleId}.namespace_prefix = [OrganizationName].[ProductName]");


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleForIncorrectNamespace, RuleForEmptyPrefix); } }


		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NamespaceDeclaration);
		}
	}
}
