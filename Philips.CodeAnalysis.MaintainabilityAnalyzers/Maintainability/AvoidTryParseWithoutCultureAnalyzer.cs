// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidTryParseWithoutCultureAnalyzer : DiagnosticAnalyzer
	{
		#region Non-Public Data Members

		private const string Title = @"Do not use TryParse without specifying a culture";
		private const string MessageFormat = @"Do not use TryParse without specifying a culture if such an overload exists.";
		private const string Description = @"Do not use TryParse without specifying a culture if such an overload exists. Failure to do so may result in code not correctly handling localized delimiters (such as commas instead of decimal points).";
		private const string Category = Categories.Maintainability;

		private const string TryParseMethodName = @"TryParse";
		private static readonly HashSet<string> _cultureParameterTypes = new()
		{
			@"IFormatProvider",
			@"CultureInfo"
		};

		#endregion

		#region Non-Public Properties/Methods

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			InvocationExpressionSyntax invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;
			if (invocationExpressionSyntax == null)
			{
				return;
			}

			// Ignore any methods not named TryParse.
			if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax || memberAccessExpressionSyntax.Name.ToString() != TryParseMethodName)
			{
				return;
			}

			// If the invoked method contains an IFormatProvider parameter, stop analyzing.
			if (context.SemanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol is not IMethodSymbol invokedMethod || HasCultureParameter(invokedMethod))
			{
				return;
			}

			// Only display an error if the class implements an overload of TryParse that accepts IFormatProvider.
			if (context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol is not IMethodSymbol methodSymbol)
			{
				return;
			}

			ImmutableArray<ISymbol> members = methodSymbol.ContainingType.GetMembers();
			IEnumerable<ISymbol> tryParseOverloads = members.Where(x => x.Name.StartsWith(TryParseMethodName));

			foreach (ISymbol member in tryParseOverloads)
			{
				if (member is IMethodSymbol method && HasCultureParameter(method))
				{
					// There is an overload that can accept culture as a parameter. Display an error.
					var location = invocationExpressionSyntax.GetLocation();
					Diagnostic diagnostic = Diagnostic.Create(Rule, location);
					context.ReportDiagnostic(diagnostic);
					return;
				}
			}
		}

		private static bool HasCultureParameter(IMethodSymbol method)
		{
			foreach (IParameterSymbol parameter in method.Parameters)
			{
				if (_cultureParameterTypes.Contains(parameter.Type.Name))
				{
					return true;
				}
			}

			return false;
		}

		#endregion

		#region Public Interface

		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidTryParseWithoutCulture), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
		}

		#endregion
	}
}
