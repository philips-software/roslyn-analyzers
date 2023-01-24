using System;
using System.Collections.Generic;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	public sealed class NamespaceIgnoringComparer : IEqualityComparer<string>
	{
		public int Compare(string x, string y)
		{
			int dotX = Math.Max(x.LastIndexOf(".", StringComparison.Ordinal) + 1, 0);
			string comparableX = x.Substring(dotX);
			int dotY = Math.Max(y.LastIndexOf(".", StringComparison.Ordinal) + 1, 0);
			string comparableY = y.Substring( dotY);
			return StringComparer.Ordinal.Compare(comparableX, comparableY);
		}

		public bool Equals(string x, string y)
		{
			if (x == null)
			{
				return y == null;
			}
			if (y == null)
			{
				return false;
			}
			return Compare(x, y) == 0;
		}

		public int GetHashCode(string obj)
		{
			return obj.GetHashCode();
		}
	}
}
