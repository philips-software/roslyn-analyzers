﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.Common
{
	public abstract class SingleDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		public DiagnosticId DiagnosticId { get; }
		public string Id { get; }
		protected Helper Helper { get; }
		protected DiagnosticDescriptor Rule { get; }

		protected SingleDiagnosticAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
											Helper helper = null, DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = true)
		{
			DiagnosticId = id;
			Id = Helper.ToDiagnosticId(id);
			Rule = new(Id, title, messageFormat, category, severity, isEnabled, description);
			Helper = helper;
		}
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	}
}
