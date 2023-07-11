// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class GeneratedCodeDetector
	{
		private readonly CodeFixHelper _helper;

		private const string AttributeName = @"GeneratedCode";
		private const string FullAttributeName = @"System.CodeDom.Compiler.GeneratedCodeAttribute";

		internal GeneratedCodeDetector(CodeFixHelper helper)
		{
			_helper = helper;
		}

		private bool HasGeneratedCodeAttribute(SyntaxNode inputNode, Func<SemanticModel> getSemanticModel)
		{
			SyntaxNode node = inputNode;
			while (node != null)
			{
				SyntaxList<AttributeListSyntax> attributes;
				switch (node)
				{
					case ClassDeclarationSyntax cls:
						attributes = cls.AttributeLists;
						break;
					case StructDeclarationSyntax st:
						attributes = st.AttributeLists;
						break;
					case MethodDeclarationSyntax method:
						attributes = method.AttributeLists;
						break;
					default:
						node = node.Parent;
						continue;
				}

				if (_helper.ForAttributes.HasAttribute(attributes, getSemanticModel, AttributeName, FullAttributeName, out _, out _))
				{
					return true;
				}

				node = node.Parent;
			}

			return false;
		}


		public bool IsGeneratedCode(OperationAnalysisContext context)
		{
			var myFilePath = context.Operation.Syntax.SyntaxTree.FilePath;
			return IsGeneratedCode(myFilePath) || HasGeneratedCodeAttribute(context.Operation.Syntax, () => { return context.Operation.SemanticModel; });
		}


		public bool IsGeneratedCode(SyntaxNodeAnalysisContext context)
		{
			var myFilePath = context.Node.SyntaxTree.FilePath;
			return IsGeneratedCode(myFilePath) || HasGeneratedCodeAttribute(context.Node, () => { return context.SemanticModel; });
		}

		public bool IsGeneratedCode(SyntaxTreeAnalysisContext context)
		{
			var myFilePath = context.Tree.FilePath;
			return IsGeneratedCode(myFilePath);
		}

		public bool IsGeneratedCode(string filePath)
		{
			var fileName = _helper.ForAssemblies.GetFileName(filePath);
			// Various Microsoft tools generate files with this postfix.
			var isDesignerFile = fileName.EndsWith(@".Designer.cs", StringComparison.OrdinalIgnoreCase);
			// WinForms generate files with this postfix.
			var isGeneratedFile = fileName.EndsWith(@".g.cs", StringComparison.OrdinalIgnoreCase);
			// Visual Studio generates SuppressMessage attributes in this file.
			var isSuppressionsFile = fileName.EndsWith(@"GlobalSuppressions.cs", StringComparison.OrdinalIgnoreCase);
			return isDesignerFile || isGeneratedFile || isSuppressionsFile;
		}

	}
}
