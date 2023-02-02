// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.Test.Helpers
{
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
				locations ??= Array.Empty<DiagnosticResultLocation>();
				return locations;
			}

			set => locations = value;
		}

		public DiagnosticResultLocation Location
		{
			get => Locations[0];
			set => Locations = new[] { value };
		}

		public DiagnosticSeverity Severity { get; set; }

		public string Id { get; set; }

		public Regex Message { get; set; }

		public string Path => Locations.Length > 0 ? Locations[0].Path : "";

		public int? Line => Locations.Length > 0 ? Locations[0].Line : -1;

		public int? Column => Locations.Length > 0 ? Locations[0].Column : -1;
	}
	
	internal static class DiagnosticResultHelper
	{
		public static DiagnosticResult Create(DiagnosticId diagnosticId, Regex message = null)
		{
			return new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(diagnosticId),
				Location = new DiagnosticResultLocation(null),
				Message = message ?? new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
			};
		}

		public static DiagnosticResult[] Create(params DiagnosticId[] diagnosticIds)
		{
			List<DiagnosticResult> diagnosticResults = new();
			foreach(DiagnosticId diagnosticId in diagnosticIds)
			{
				DiagnosticResult dr = Create(diagnosticId);
				diagnosticResults.Add(dr);
			}
			return diagnosticResults.ToArray();
		}
	}
}
