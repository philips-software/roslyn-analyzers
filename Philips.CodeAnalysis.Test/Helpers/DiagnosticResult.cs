// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.Test
{
	/// <summary>
	/// Location where the diagnostic appears, as determined by path, line number, and column number.
	/// </summary>
	public struct DiagnosticResultLocation
	{
		public DiagnosticResultLocation(int? line) : this(null, line, null)
		{
		}

		public DiagnosticResultLocation(string path, int? line, int? column)
		{
			if (line.HasValue && line < -1)
			{
				throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
			}

			if (column.HasValue && column < -1)
			{
				throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
			}

			this.Path = path;
			this.Line = line;
			this.Column = column;
		}

		public string Path { get; }
		public int? Line { get; }
		public int? Column { get; }
	}

	/// <summary>
	/// Struct that stores information about a Diagnostic appearing in a source
	/// </summary>
	public struct DiagnosticResult
	{
		private DiagnosticResultLocation[] locations;

		public DiagnosticResultLocation[] Locations
		{
			get
			{
				if (this.locations == null)
				{
					this.locations = new DiagnosticResultLocation[] { };
				}
				return this.locations;
			}

			set
			{
				this.locations = value;
			}
		}

		public DiagnosticResultLocation Location
		{
			get { return Locations[0]; }
			set
			{
				Locations = new[] { value };
			}
		}

		public DiagnosticSeverity Severity { get; set; }

		public string Id { get; set; }

		public Regex Message { get; set; }

		public string Path
		{
			get
			{
				return this.Locations.Length > 0 ? this.Locations[0].Path : "";
			}
		}

		public int? Line
		{
			get
			{
				return this.Locations.Length > 0 ? this.Locations[0].Line : -1;
			}
		}

		public int? Column
		{
			get
			{
				return this.Locations.Length > 0 ? this.Locations[0].Column : -1;
			}
		}
	}

	internal static class DiagnosticResultHelper
	{
		public static DiagnosticResult Create(DiagnosticIds diagnosticId, Regex message = null)
		{
			return new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(diagnosticId),
				Location = new DiagnosticResultLocation(null),
				Message = message ?? new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
			};
		}

		public static DiagnosticResult[] CreateArray(DiagnosticIds diagnosticId, Regex message = null)
		{
			return new DiagnosticResult[]
			{
				Create(diagnosticId, message),
			};
		}
	}
}
