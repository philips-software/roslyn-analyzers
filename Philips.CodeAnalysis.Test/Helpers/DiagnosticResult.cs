// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.Test.Helpers
{
	/// <summary>
	/// Struct that stores information about a Diagnostic appearing in a source
	/// </summary>
	public struct DiagnosticResult
	{
		private DiagnosticResultLocation[] locations;

		public IReadOnlyCollection<DiagnosticResultLocation> Locations
		{
			get
			{
				locations ??= Array.Empty<DiagnosticResultLocation>();
				return locations;
			}

			set => locations = value.ToArray();
		}

		public DiagnosticResultLocation Location
		{
			get => locations[0];
			set => locations = new[] { value };
		}

		public DiagnosticSeverity Severity { get; set; }

		public string Id { get; set; }

		public Regex Message { get; set; }

		public string Path => locations.Length > 0 ? locations[0].Path : "";

		public int? Line => locations.Length > 0 ? locations[0].Line : -1;

		public int? Column => locations.Length > 0 ? locations[0].Column : -1;
	}
}
