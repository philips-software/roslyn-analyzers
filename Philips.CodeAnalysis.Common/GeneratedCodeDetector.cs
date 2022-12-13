// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class GeneratedCodeDetector
	{
		private const string AttributeName = @"GeneratedCode";
		private readonly string FullAttributeName = @"System.CodeDom.Compiler.GeneratedCodeAttribute";

		private bool HasGeneratedCodeAttribute(SyntaxNode node, Func<SemanticModel> getSemanticModel)
		{
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

				if (Helper.HasAttribute(attributes, getSemanticModel, AttributeName, FullAttributeName, out _, out _))
				{
					return true;
				}

				node = node.Parent;
			}

			return false;
		}


		public bool IsGeneratedCode(OperationAnalysisContext context)
		{
			string myFilePath = context.Operation.Syntax.SyntaxTree.FilePath;
			return IsGeneratedCode(myFilePath) || HasGeneratedCodeAttribute(context.Operation.Syntax, () => { return context.Operation.SemanticModel; }); ;
		}


		public bool IsGeneratedCode(SyntaxNodeAnalysisContext context)
		{
			string myFilePath = context.Node.SyntaxTree.FilePath;
			return IsGeneratedCode(myFilePath) || HasGeneratedCodeAttribute(context.Node, () => { return context.SemanticModel; });
		}

		public bool IsGeneratedCode(SyntaxTreeAnalysisContext context)
		{
			string myFilePath = context.Tree.FilePath;
			return IsGeneratedCode(myFilePath);
		}

		public bool IsGeneratedCode(string filePath)
		{
			string fileName = Helper.GetFileName(filePath);
			bool isDesignerFile = fileName.EndsWith(@".Designer.cs", StringComparison.OrdinalIgnoreCase);
			bool isGeneratedFile = fileName.EndsWith(@".g.cs", StringComparison.OrdinalIgnoreCase);
			return isDesignerFile || isGeneratedFile;
		}

	}
}
