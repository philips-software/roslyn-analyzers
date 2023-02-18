// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Philips.CodeAnalysis.Common
{
	public sealed class NamespaceIgnoringComparer : IEqualityComparer<string>
	{
		private const string Dot = ".";

		public int Compare(string x, string y)
		{
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}
			var dotX = Math.Max(x.LastIndexOf(Dot, StringComparison.Ordinal) + 1, 0);
			var comparableX = x.Substring(dotX);

			var dotY = Math.Max(y.LastIndexOf(Dot, StringComparison.Ordinal) + 1, 0);
			var comparableY = y.Substring(dotY);

			return StringComparer.Ordinal.Compare(comparableX, comparableY);
		}

		public bool Equals(string x, string y)
		{
			return Compare(x, y) == 0;
		}

		public int GetHashCode(string obj)
		{
			return obj.GetHashCode();
		}
	}
}
