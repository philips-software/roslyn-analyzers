// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NamespaceMatchFilePathAnalyzer : SingleDiagnosticAnalyzer<NamespaceDeclarationSyntax, NamespaceMatchFilePathSyntaxNodeAction>
	{
		public AdditionalFilesHelper AdditionalFilesHelper { get; }

		private const string Title = @"Namespace matches File Path";
		private const string MessageFormat = @"Namespace and File Path must match";
		private const string Description = @"In order to prevent pollution of namespaces, and maintainability of namespaces, the File Path and Namespace must match. To include subfolders in the namespace, add 'dotnet_code_quality.PH2006.folder_in_namespace = true' to the .editorconfig.";

		public NamespaceMatchFilePathAnalyzer()
			: this(null)
		{ }

		public NamespaceMatchFilePathAnalyzer(AdditionalFilesHelper additionalFilesHelper)
			: base(DiagnosticId.NamespaceMatchFilePath, Title, MessageFormat, Description, Categories.Naming)
		{
			AdditionalFilesHelper = additionalFilesHelper;
		}
	}

	public class NamespaceMatchFilePathSyntaxNodeAction : SyntaxNodeAction<NamespaceDeclarationSyntax>
	{
		private bool _isFolderInNamespace;
		private bool _isConfigInitialized;

		public override void Analyze()
		{
			string myNamespace = Node.Name.ToString();
			string myFilePath = Context.Node.SyntaxTree.FilePath;

			InitializeConfiguration();

			if (_isFolderInNamespace)
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

			if (Helper.IsNamespaceExempt(myNamespace))
			{
				return;
			}

			var location = Node.Name.GetLocation();
			ReportDiagnostic(location);
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

		private void InitializeConfiguration()
		{
			if (!_isConfigInitialized)
			{
				var additionalFilesHelper = (Analyzer as NamespaceMatchFilePathAnalyzer).AdditionalFilesHelper;
				additionalFilesHelper ??= new AdditionalFilesHelper(Context.Options, Context.Compilation);
				string folderInNamespace = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"folder_in_namespace");
				_ = bool.TryParse(folderInNamespace, out _isFolderInNamespace);
				_isConfigInitialized = true;
			}
		}
	}
}
