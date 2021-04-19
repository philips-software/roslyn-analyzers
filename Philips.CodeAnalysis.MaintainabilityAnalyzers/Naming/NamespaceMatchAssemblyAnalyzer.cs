// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NamespaceMatchAssemblyAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Namespace matches File Path";
		private const string MessageFormat = @"Namespace, File Path, Assembly, and Project must all match";
		private const string Description = @"In order to prevent pollution of namespaces, and maintainability of namespaces, the File Path, Assembly, Project, and Namespace must all match";
		private const string Category = Categories.Naming;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.NamespaceMatchAssembly), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NamespaceDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (Helper.IsGeneratedCode(context))
			{
				return;
			}

			NamespaceDeclarationSyntax namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
			string myNamespace = namespaceDeclaration.Name.ToString();
			string myFilePath = context.Node.SyntaxTree.FilePath;
			string[] nodes = myFilePath.Split('/', '\\');

			for (int i = nodes.Length - 2; i > 0; i--)  // Exclude file.cs (i.e., the end) and the drive (i.e., the start).  Start from back to succeed quickly.
			{
				if (string.Compare(nodes[i], myNamespace, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return;
				}
			}
			Diagnostic diagnostic = Diagnostic.Create(Rule, namespaceDeclaration.Name.GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}
