// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.SecurityAnalyzers
{
	/// <summary>
	/// Tracks information about parameter flow through methods
	/// </summary>
	internal sealed class ParameterFlowInfo
	{
		/// <summary>
		/// The parameter being tracked
		/// </summary>
		public IParameterSymbol Parameter { get; set; }

		/// <summary>
		/// Whether the parameter has been validated
		/// </summary>
		public bool IsValidated { get; set; }

		/// <summary>
		/// Locations where the parameter is used
		/// </summary>
		public List<Location> UsageLocations { get; set; }

		/// <summary>
		/// Methods that the parameter flows to
		/// </summary>
		public List<IMethodSymbol> FlowsToMethods { get; set; }
	}

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class InputValidationAnalyzer : DiagnosticAnalyzerBase
	{
		private const string Title = @"Regex needs a timeout";
		public const string MessageFormat = @"When constructing a new Regex instance, provide a timeout.";
		private const string Description = @"When constructing a new Regex instance, provide a timeout (or `RegexOptions.NonBacktracking` in .NET 7 and higher) as this can facilitate denial-of-serice attacks.";
		private const string Category = Categories.Security;

		// private static readonly Dictionary<string, ValidationSinkInfo> SinkMemberInfos = [];
		// private readonly List<SyntaxNode> validationSinks = [];

		public static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId.InputValidation.ToId(),
			Title, MessageFormat, Category,
			DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
		}

		private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
		{
			var method = (MethodDeclarationSyntax)context.Node;
			IMethodSymbol methodSymbol = context.SemanticModel.GetDeclaredSymbol(method);
			if (methodSymbol == null)
			{
				return;
			}

			if (
				(methodSymbol.ReturnType.SpecialType != SpecialType.System_String) &&
				!methodSymbol.Parameters.Any(parameter => parameter.Type.SpecialType == SpecialType.System_String))
			{
				return;
			}

			// Get method references and analyze parameter flow
			AnalyzeParameterFlow(context, methodSymbol);
		}

		private void AnalyzeParameterFlow(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol)
		{
			// Skip methods without a body (interface methods, abstract methods, etc.)
			if (methodSymbol.DeclaringSyntaxReferences.Length == 0)
			{
				return;
			}

			// Get method syntax node
			SyntaxNode methodSyntax = methodSymbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
			if (methodSyntax is not MethodDeclarationSyntax methodDeclaration)
			{
				return;
			}

			// Get the method body
			if (methodDeclaration.Body == null)
			{
				return;
			}

			// Create a dictionary to track parameter usage
			Dictionary<string, ParameterFlowInfo> parameterTracker = [];

			// Initialize tracking for each string parameter
			foreach (IParameterSymbol parameter in methodSymbol.Parameters)
			{
				if (parameter.Type.SpecialType == SpecialType.System_String)
				{
					parameterTracker.Add(parameter.Name, new ParameterFlowInfo
					{
						Parameter = parameter,
						IsValidated = false,
						UsageLocations = [],
						FlowsToMethods = []
					});
				}
			}

			// Find all invocations within the method body
			var invocations = methodDeclaration.Body.DescendantNodes()
				.OfType<InvocationExpressionSyntax>()
				.ToList();

			// Analyze each invocation for parameter usage
			foreach (InvocationExpressionSyntax invocation in invocations)
			{
				AnalyzeInvocationForParameterUsage(context, invocation, parameterTracker);
			}

			// Check for validation before usage
			foreach (ParameterFlowInfo paramInfo in parameterTracker.Values)
			{
				if (paramInfo.Parameter.Type.SpecialType == SpecialType.System_String && !paramInfo.IsValidated &&
					paramInfo.UsageLocations.Count > 0)
				{
					// Report diagnostic for unvalidated string parameter usage
					context.ReportDiagnostic(Diagnostic.Create(
						Rule,
						paramInfo.UsageLocations.First(),
						paramInfo.Parameter.Name));
				}
			}
		}

		private void AnalyzeInvocationForParameterUsage(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocation,
			Dictionary<string, ParameterFlowInfo> parameterTracker)
		{
			// Get method symbol for the invocation using the context's semantic model
			SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
			if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
			{
				return;
			}

			// Check if this is a validation method
			var isValidationMethod = IsValidationMethod(methodSymbol);

			// Analyze arguments
			for (var i = 0; i < invocation.ArgumentList.Arguments.Count && i < methodSymbol.Parameters.Length; i++)
			{
				ArgumentSyntax argument = invocation.ArgumentList.Arguments[i];

				// Check if argument is a parameter reference
				if (argument.Expression is IdentifierNameSyntax identifier)
				{
					var paramName = identifier.Identifier.ValueText;

					if (parameterTracker.TryGetValue(paramName, out ParameterFlowInfo paramInfo))
					{
						// Record usage location
						paramInfo.UsageLocations.Add(argument.GetLocation());
						paramInfo.FlowsToMethods.Add(methodSymbol);
						paramInfo.IsValidated = isValidationMethod;
						// Check if this is a known sink method
						if (IsSinkMethod(methodSymbol) && !paramInfo.IsValidated)
						{
							// Report diagnostic for unvalidated parameter flowing to sink
							context.ReportDiagnostic(Diagnostic.Create(
								Rule,
								argument.GetLocation(),
								$"Parameter '{paramName}' is used without validation"));
						}
					}
				}
			}
		}

		private bool IsValidationMethod(IMethodSymbol methodSymbol)
		{
			// Check if method name suggests validation
			var methodName = methodSymbol.Name.ToLowerInvariant();
			return methodName.Contains("validate") || methodName.Contains("sanitize") ||
				   methodName.Contains("check") || methodName.Contains("verify");
		}
		/*
				private bool IsSourceMethod(IMethodSymbol methodSymbol)
				{
					// Check if method is a known sink (e.g., database access, file I/O, etc.)
					var fullName = $"{methodSymbol.ContainingNamespace}.{methodSymbol.ContainingType.Name}.{methodSymbol.Name}".ToLowerInvariant();

					// Check for common source patterns
					return fullName.Contains("control") || fullName.Contains("window") ||
						   fullName.Contains("keyboard") || fullName.Contains("mouse");
				}
		*/
		private bool IsSinkMethod(IMethodSymbol methodSymbol)
		{
			// Check if method is a known sink (e.g., database access, file I/O, etc.)
			var fullName = $"{methodSymbol.ContainingNamespace}.{methodSymbol.ContainingType.Name}.{methodSymbol.Name}".ToLowerInvariant();

			// Check for common sink patterns
			return fullName.Contains("SqlCommand") || fullName.Contains("Execute") ||
				   fullName.Contains("Query") || fullName.Contains("Write") ||
				   fullName.Contains("Read") || fullName.Contains("Load") ||
				   fullName.Contains("Save");
		}

		protected override GeneratedCodeAnalysisFlags GetGeneratedCodeAnalysisFlags()
		{
			return GeneratedCodeAnalysisFlags.None;
		}
	}

	internal sealed class ValidationSinkInfo
	{
		private readonly string _namespace;
		private readonly string _typeName;

		public ValidationSinkInfo(string ns, string type, string method, int index)
		{
			_namespace = ns;
			_typeName = type;
			MethodName = method;
			ArgumentIndex = index;
		}

		public string MethodName { get; }

		public int ArgumentIndex { get; }

		public bool IsMatch(IPreprocessingSymbol symbol)
		{
			return
				string.CompareOrdinal(symbol.ContainingNamespace.Name, _namespace) == 0 &&
				string.CompareOrdinal(symbol.ContainingType.Name, _typeName) == 0;
		}
	}
}
