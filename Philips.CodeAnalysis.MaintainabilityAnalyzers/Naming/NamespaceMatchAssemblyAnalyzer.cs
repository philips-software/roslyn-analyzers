// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
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
		private const string Description = @"In order to prevent pollution of namespaces, and maintainability of namespaces, the File Path, Assembly, Project, and Namespace must all match. To include subfolders in the namespace, add 'dotnet_code_quality.PH2006.folder_in_namespace = true' to the .editorconfig.";
		private const string Category = Categories.Naming;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.NamespaceMatchAssembly), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		private readonly GeneratedCodeAnalysisFlags _generatedCodeFlags;
		private AdditionalFilesHelper _additionalFilesHelper;
		private bool _folderInNamespace;
		private bool _configInitialized;

		public NamespaceMatchAssemblyAnalyzer()
			: this(GeneratedCodeAnalysisFlags.None, null)
		{ }

		public NamespaceMatchAssemblyAnalyzer(GeneratedCodeAnalysisFlags generatedCodeFlags, AdditionalFilesHelper additionalFilesHelper)
		{
			_generatedCodeFlags = generatedCodeFlags;
			_additionalFilesHelper = additionalFilesHelper;
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(_generatedCodeFlags);
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
			string myFilePath = context.Node.SyntaxTree.FilePath;

			InitializeConfiguration(context);

			if (_folderInNamespace)
			{
				// Does the namespace exactly match the trailing folders?
				if (DoesFilePathEndWithNamespace(myNamespace, myFilePath))
				{
					return;
				}
			}
			else
			{
				// Does the namespace exactly match one of the folders in the path?
				if (IsNamespacePartOfPath(myNamespace, myFilePath))
				{
					return;
				}
			}

			// TODO: Check assembly name, see issue #174

			var location = namespaceDeclaration.Name.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rule, location);
			context.ReportDiagnostic(diagnostic);
		}

		private bool IsNamespacePartOfPath(string ns, string path)
		{
			string[] nodes = path.Split(Path.DirectorySeparatorChar);
			for (int i = nodes.Length - 2; i > 0; i--)  // Exclude file.cs (i.e., the end) and the drive (i.e., the start).  Start from back to succeed quickly.
			{
				if (string.Equals(nodes[i], ns, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		private bool DoesFilePathEndWithNamespace(string ns, string path)
		{
			string folder = Path.GetDirectoryName(path);
			string allowedNamespace = folder.Replace(Path.DirectorySeparatorChar, '.');
			return allowedNamespace.EndsWith(ns, StringComparison.OrdinalIgnoreCase);
		}

		private void InitializeConfiguration(SyntaxNodeAnalysisContext context)
		{
			if (!_configInitialized)
			{
				_additionalFilesHelper ??= new AdditionalFilesHelper(context.Options, context.Compilation);
				string folderInNamespace = _additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"folder_in_namespace");
				_ = bool.TryParse(folderInNamespace, out _folderInNamespace);
				_configInitialized = true;
			}
		}
	}
}
