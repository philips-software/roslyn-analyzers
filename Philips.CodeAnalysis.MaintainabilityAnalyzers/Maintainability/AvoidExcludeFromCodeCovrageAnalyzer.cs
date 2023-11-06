﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidExcludeFromCodeCoverageAnalyzer : SingleDiagnosticAnalyzer<AttributeSyntax, AvoidExcludeFromCodeCoverageAnalyzerSyntaxNodeAction>
	{
		private const string Title = @"Avoid the ExcludeFromCodeCoverage attribute";
		public const string MessageFormat = Title;
		private const string Description = MessageFormat;

		public AvoidExcludeFromCodeCoverageAnalyzer()
			: base(DiagnosticId.AvoidExcludeFromCodeCoverage, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class AvoidExcludeFromCodeCoverageAnalyzerSyntaxNodeAction : SyntaxNodeAction<AttributeSyntax>
	{
		private const string ExcludeFromCodeCoverageAttributeTypeName = "ExcludeFromCodeCoverage";
		public override void Analyze()
		{
			IReadOnlyDictionary<string, string> aliases = Helper.ForNamespaces.GetUsingAliases(Node);
			if (!Node.Name.GetFullName(aliases).Contains(ExcludeFromCodeCoverageAttributeTypeName))
			{
				return;
			}

			Location location = Node.GetLocation();
			ReportDiagnostic(location);
		}
	}
}
