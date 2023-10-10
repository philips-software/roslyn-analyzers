// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public class NamespacesHelper
	{
		internal NamespacesHelper()
		{
			// Hide the constructor.
		}

		public bool IsNamespaceExempt(string myNamespace)
		{
			// https://developercommunity.visualstudio.com/t/error-cs0518-predefined-type-systemruntimecompiler/1244809
			List<string> exceptions = new() { "System.Runtime.CompilerServices" };
			return exceptions.Contains(myNamespace);
		}

		public IReadOnlyDictionary<string, string> GetUsingAliases(SyntaxNode node)
		{
			var list = new Dictionary<string, string>();
			SyntaxNode root = node.SyntaxTree.GetRoot();
			foreach (UsingDirectiveSyntax child in root.DescendantNodes(n => n is not TypeDeclarationSyntax).OfType<UsingDirectiveSyntax>())
			{
				if (child.Alias != null)
				{
					var alias = child.Alias.Name.GetFullName(list);
					var name = child.Name.GetFullName(list);
					list.Add(alias, name);
				}
			}
			return list;
		}
	}
}
