// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

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
			List<string> exceptions = ["System.Runtime.CompilerServices"];
			return exceptions.Contains(myNamespace);
		}

		public NamespaceResolver GetUsingAliases(SyntaxNode node)
		{
			return new NamespaceResolver(node);
		}
	}
}
