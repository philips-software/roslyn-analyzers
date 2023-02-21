// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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

			IReadOnlyDictionary<string, string> aliases = Helper.GetUsingAliases(Node);

			IEnumerable<InvocationExpressionSyntax> invocations = Node.DescendantNodes().OfType<InvocationExpressionSyntax>();
			ExceptionWalker walker = new();
			List<string> unhandledExceptions = new();
			foreach (InvocationExpressionSyntax invocation in invocations)
			{
				IEnumerable<string> newExceptions = walker.UnhandledFromInvocation(invocation, aliases, Context.SemanticModel);
				if (newExceptions.Any())
				{
					unhandledExceptions.AddRange(newExceptions);
				}
			}


			// List the documented exception types.
			var docHelper = new DocumentationHelper(Node);
			IEnumerable<string> documentedExceptions = docHelper.GetExceptionCrefs();
			var comparer = new NamespaceIgnoringComparer();
			IEnumerable<string> remainingExceptions =
				unhandledExceptions.Where(ex =>
					documentedExceptions.All(doc => comparer.Compare(ex, doc) != 0));
			if (remainingExceptions.Any())
			{
				Location loc = Node.Identifier.GetLocation();
				string methodName = Node.Identifier.Text;
				string remainingExceptionsString = string.Join(",", remainingExceptions);
				ImmutableDictionary<string, string> properties = ImmutableDictionary<string, string>.Empty.Add(StringConstants.ThrownExceptionPropertyKey, remainingExceptionsString);
				var diagnostic = Diagnostic.Create(Rule, loc, properties, methodName, remainingExceptionsString);
				Context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
