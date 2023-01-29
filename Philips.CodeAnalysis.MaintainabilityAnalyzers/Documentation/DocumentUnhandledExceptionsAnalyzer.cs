// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	/// <summary>
	/// Analyzer that checks if the text of the XML code documentation contains a reference to each exception being unhandled in the method or property.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DocumentUnhandledExceptionsAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Document unhandled exceptions";
		private const string MessageFormat = @"Document that this method can throw from {0} the following exceptions: {1}.";
		private const string Description = @"Be clear to your callers what exception can be thrown from your method (or any called methods) by mentioning each of them in an <exception> element in the documentation of the method.";
		private const string Category = Categories.Documentation;

		private static class WellKnownExceptions
		{
			public const string Exception = "System.Exception";
			public const string ArgumentException = "System.ArgumentException";
			public const string ArgumentNullException = "System.ArgumentNullException";
			public const string ArgumentOutOfRangeException = "System.ArgumentOutOfRangeException";
			public const string DirectoryNotFoundException = "System.IO.DirectoryNotFoundException";
			public const string IoException = "System.IO.IOException";
			public const string NotSupportedException = "System.NotSupportedException";
			public const string PathTooLongException = "System.IO.PathTooLongException";
			public const string SecurityException = "System.SecurityException";
			public const string UnauthorizedException = "System.UnauthorizedException";
		}

		private static readonly DiagnosticDescriptor Rule =
			new(Helper.ToDiagnosticId(DiagnosticIds.DocumentUnhandledExceptions), Title, MessageFormat, Category,
				DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		private readonly Helper _helper;

		public DocumentUnhandledExceptionsAnalyzer()
			: this(new Helper())
		{ }
		public DocumentUnhandledExceptionsAnalyzer(Helper helper)
		{
			_helper = helper;
		}

		private static readonly Regex DocumentationRegex = new("exception\\scref=\\\"(.*)\\\"", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		private static readonly Dictionary<string, string[]> ExceptionsMap = new()
		{
			{
				"System.IO.Directory.CreateDirectory",
				new[]
				{
					WellKnownExceptions.IoException, WellKnownExceptions.UnauthorizedException, WellKnownExceptions.ArgumentException, WellKnownExceptions.ArgumentNullException, WellKnownExceptions.PathTooLongException, WellKnownExceptions.DirectoryNotFoundException, WellKnownExceptions.NotSupportedException
				}
			},
			{
				"System.IO.Directory.Delete",
				new[]
				{
					WellKnownExceptions.IoException, WellKnownExceptions.UnauthorizedException, WellKnownExceptions.ArgumentException, WellKnownExceptions.ArgumentNullException, WellKnownExceptions.PathTooLongException, WellKnownExceptions.DirectoryNotFoundException
				}
			},
			{
				"System.IO.Directory.GetFiles",
				new[]
				{
					WellKnownExceptions.IoException, WellKnownExceptions.ArgumentOutOfRangeException, WellKnownExceptions.ArgumentException, WellKnownExceptions.ArgumentNullException, WellKnownExceptions.PathTooLongException, WellKnownExceptions.DirectoryNotFoundException, WellKnownExceptions.SecurityException
				}
			},
			{
				"System.IO.Directory.GetDirectories",
				new[]
				{
					WellKnownExceptions.IoException, WellKnownExceptions.ArgumentException, WellKnownExceptions.ArgumentNullException, WellKnownExceptions.PathTooLongException, WellKnownExceptions.DirectoryNotFoundException
				}
			},
			{
				"System.IO.Directory.EnumerateFiles",
				new[]
				{
					WellKnownExceptions.IoException, WellKnownExceptions.ArgumentOutOfRangeException, WellKnownExceptions.ArgumentException, WellKnownExceptions.ArgumentNullException, WellKnownExceptions.PathTooLongException, WellKnownExceptions.DirectoryNotFoundException, WellKnownExceptions.SecurityException
				}
			},
			{
				"System.IO.Directory.EnumerateDirectories",
				new[]
				{
					WellKnownExceptions.IoException, WellKnownExceptions.ArgumentOutOfRangeException, WellKnownExceptions.ArgumentException, WellKnownExceptions.ArgumentNullException, WellKnownExceptions.PathTooLongException, WellKnownExceptions.DirectoryNotFoundException, WellKnownExceptions.SecurityException
				}
			},
			{
				"System.IO.Directory.Move",
				new[]
				{
					WellKnownExceptions.IoException, WellKnownExceptions.ArgumentException, WellKnownExceptions.ArgumentNullException, WellKnownExceptions.PathTooLongException, WellKnownExceptions.DirectoryNotFoundException
				}
			},
		};

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var method = (MethodDeclarationSyntax)context.Node;

			// TODO: Read PDB as done in: https://github.com/microsoft/peeker

			var aliases = _helper.GetUsingAliases(method);

			var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
			List<string> unhandledExceptions = new();
			foreach (var invocation in invocations)
			{
				var newExceptions = GetFromInvocation(context, invocation, aliases);
				if (newExceptions.Any())
				{
					unhandledExceptions.AddRange(newExceptions);
				}
			}


			// List the documented exception types.
			var docHelper = new DocumentationHelper(method);
			var documentedExceptions = docHelper.GetExceptionCrefs();
			var comparer = new NamespaceIgnoringComparer();
			var remainingExceptions = unhandledExceptions.Where(ex => documentedExceptions.All(doc => comparer.Compare(ex, doc) != 0));
			if (remainingExceptions.Any())
			{
				var loc = method.Identifier.GetLocation();
				var methodName = method.Identifier.Text;
				string unhandledStr = string.Join(",", remainingExceptions);
				var remainingExceptionsString = string.Join(",", remainingExceptions);
				var properties = ImmutableDictionary<string, string>.Empty.Add("missing", remainingExceptionsString);
				Diagnostic diagnostic = Diagnostic.Create(Rule, loc, properties, methodName, unhandledStr);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private IEnumerable<string> GetFromInvocation(SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocation, IReadOnlyDictionary<string, string> aliases)
		{
			var expectedExceptions = Array.Empty<string>();
			var invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol;
			if (invokedSymbol is IMethodSymbol or IPropertySymbol)
			{
				var invokedName = GetFullName(invokedSymbol);
				if (!ExceptionsMap.TryGetValue(invokedName, out expectedExceptions))
				{
					var documentation = invokedSymbol.GetDocumentationCommentXml();
					expectedExceptions = ParseFromDocumentation(documentation);
				}
			}

			IEnumerable<string> unhandledExceptions = expectedExceptions;
			var tryStatements = invocation.Ancestors().OfType<TryStatementSyntax>();
			foreach (var tryStatement in tryStatements)
			{
				var handledExceptionTypes = tryStatement.Catches.Select(cat => cat.Declaration?.Type.GetFullName(aliases));
				unhandledExceptions = handledExceptionTypes.Any(ex => ex == WellKnownExceptions.Exception) ? Array.Empty<string>() : unhandledExceptions.Except(handledExceptionTypes);
			}

			return unhandledExceptions;
		}

		private string[] ParseFromDocumentation(string documentation)
		{
			var matches = DocumentationRegex.Matches(documentation);
			var results = new string[matches.Count];
			for(int i = 0; i < results.Length; i++)
			{
				results[i] = matches[i].Groups[1].Captures[0].Value.Trim('!', ':', 'T');
			}
			return results;
		}

		private string GetFullName(ISymbol symbol)
		{
			List<string> namespaces = new();
			var ns = symbol.ContainingNamespace;
			while(ns != null && !string.IsNullOrEmpty(ns.Name))
			{
				namespaces.Add(ns.Name);
				ns = ns.ContainingNamespace;
			}
			namespaces.Reverse();
			return $"{string.Join(".", namespaces)}.{symbol.ContainingType.Name}.{symbol.Name}";
		}
	}
}
