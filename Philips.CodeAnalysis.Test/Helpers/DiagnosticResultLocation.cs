// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;

namespace Philips.CodeAnalysis.Test.Helpers
{
	/// <summary>
	/// Location where the diagnostic appears, as determined by path, line number, and column number.
	/// </summary>
	public readonly struct DiagnosticResultLocation
	{
		public DiagnosticResultLocation(int? line) : this(null, line, null)
		{
		}

		public DiagnosticResultLocation(string path, int? line, int? column)
			: this(path, line, column, null, null)
		{ }
		public DiagnosticResultLocation(string path, int? line, int? column, int? endLine, int? endColumn)
		{
			if (line is < -1)
			{
				throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
			}

			if (column is < -1)
			{
				throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
			}

			Path = path;
			Line = line;
			Column = column;
			EndLine = endLine;
			EndColumn = endColumn;
		}

		public string Path { get; }
		public int? Line { get; }
		public int? Column { get; }

		public int? EndLine { get; }
		public int? EndColumn { get; }
	}
}
