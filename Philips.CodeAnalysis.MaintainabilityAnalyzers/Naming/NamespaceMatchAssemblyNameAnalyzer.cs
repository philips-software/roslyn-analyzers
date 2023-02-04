// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class NamespaceMatchAssemblyNameAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Namespace matches Assembly Name";
		private const string MessageFormat = @"Namespace and Assembly Name must match";
		private const string Description = @"In order to prevent pollution of namespaces, and maintainability of namespaces, the Assembly Name and Namespace must match.";
		private const string Category = Categories.Naming;

		public NamespaceMatchAssemblyNameAnalyzer() : base(DiagnosticId.NamespaceMatchAssemblyName, Title, MessageFormat, Description, Category)
		{
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NamespaceDeclaration);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			NamespaceDeclarationSyntax namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
			string myNamespace = namespaceDeclaration.Name.ToString();
			string myAssemblyName = context.Compilation?.AssemblyName;

			if (string.IsNullOrEmpty(myAssemblyName))
			{
				return;
			}

			if (!IsNamespacePartOfAssemblyName(myNamespace, myAssemblyName))
			{
				ReportDiagnostic(context, namespaceDeclaration);
			}
		}

		private bool IsNamespacePartOfAssemblyName(string ns, string assemblyName)
		{
			return ns.StartsWith(assemblyName, StringComparison.OrdinalIgnoreCase);
		}

		private void ReportDiagnostic(SyntaxNodeAnalysisContext context,
			NamespaceDeclarationSyntax namespaceDeclaration)
		{
			var location = namespaceDeclaration.Name.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rule, location);
			context.ReportDiagnostic(diagnostic);
		}
	}
}
