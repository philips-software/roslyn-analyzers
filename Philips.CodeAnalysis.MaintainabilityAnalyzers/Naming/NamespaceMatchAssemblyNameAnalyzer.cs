﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NamespaceMatchAssemblyNameAnalyzer : SingleDiagnosticAnalyzer<NamespaceDeclarationSyntax, NamespaceMatchAssemblyNameSyntaxNodeAction>
	{
		private const string Title = @"Namespace matches Assembly Name";
		private const string MessageFormat = @"Namespace and Assembly Name must match";
		private const string Description = @"In order to prevent pollution of namespaces, and maintainability of namespaces, the Assembly Name and Namespace must match.";

		public NamespaceMatchAssemblyNameAnalyzer()
			: base(DiagnosticId.NamespaceMatchAssemblyName, Title, MessageFormat, Description, Categories.Naming, isEnabled: false)
		{ }
	}

	public class NamespaceMatchAssemblyNameSyntaxNodeAction : SyntaxNodeAction<NamespaceDeclarationSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			var myNamespace = Node.Name.ToString();
			var myAssemblyName = Context.Compilation?.AssemblyName;

			if (string.IsNullOrEmpty(myAssemblyName))
			{
				return Option<Diagnostic>.None;
			}

			if (IsNamespacePartOfAssemblyName(myNamespace, myAssemblyName))
			{
				return Option<Diagnostic>.None;
			}

			if (Helper.IsNamespaceExempt(myNamespace))
			{
				return Option<Diagnostic>.None;
			}
			Location location = Node.Name.GetLocation();
			return PrepareDiagnostic(location).ToSome();
		}

		private bool IsNamespacePartOfAssemblyName(string ns, string assemblyName)
		{
			return ns.StartsWith(assemblyName, StringComparison.OrdinalIgnoreCase);
		}

	}
}
