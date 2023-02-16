﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	/// <summary>
	/// Analyzer that checks if the text of the XML code documentation contains a reference to each exception being unhandled in the method or property.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DocumentUnhandledExceptionsAnalyzer : SingleDiagnosticAnalyzer<MethodDeclarationSyntax, DocumentUnhandledExceptionsSyntaxNodeAction>
	{
		private const string Title = @"Document unhandled exceptions";
		private const string MessageFormat = @"Document that this method can throw from {0} the following exceptions: {1}.";
		private const string Description = @"Be clear to your callers what exception can be thrown from your method (or any called methods) by mentioning each of them in an <exception> element in the documentation of the method.";

		public DocumentUnhandledExceptionsAnalyzer()
			: base(DiagnosticId.DocumentUnhandledExceptions, Title, MessageFormat, Description, Categories.Documentation, isEnabled: false)
		{ }
	}

	public class DocumentUnhandledExceptionsSyntaxNodeAction : SyntaxNodeAction<MethodDeclarationSyntax>
	{
		public override void Analyze()
		{
			if (Context.Compilation?.SyntaxTrees.FirstOrDefault()?.Options.DocumentationMode == DocumentationMode.None)
			{
				return;
			}

			var aliases = Helper.GetUsingAliases(Node);

			var invocations = Node.DescendantNodes().OfType<InvocationExpressionSyntax>();
			ExceptionWalker walker = new();
			List<string> unhandledExceptions = new();
			foreach (var invocation in invocations)
			{
				var newExceptions = walker.UnhandledFromInvocation(invocation, aliases, Context.SemanticModel);
				if (newExceptions.Any())
				{
					unhandledExceptions.AddRange(newExceptions);
				}
			}


			// List the documented exception types.
			var docHelper = new DocumentationHelper(Node);
			var documentedExceptions = docHelper.GetExceptionCrefs();
			var comparer = new NamespaceIgnoringComparer();
			var remainingExceptions =
				unhandledExceptions.Where(ex =>
					documentedExceptions.All(doc => comparer.Compare(ex, doc) != 0));
			if (remainingExceptions.Any())
			{
				var loc = Node.Identifier.GetLocation();
				var methodName = Node.Identifier.Text;
				var remainingExceptionsString = string.Join(",", remainingExceptions);
				var properties = ImmutableDictionary<string, string>.Empty.Add(StringConstants.ThrownExceptionPropertyKey, remainingExceptionsString);
				Diagnostic diagnostic = Diagnostic.Create(Rule, loc, properties, methodName, remainingExceptionsString);
				Context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
