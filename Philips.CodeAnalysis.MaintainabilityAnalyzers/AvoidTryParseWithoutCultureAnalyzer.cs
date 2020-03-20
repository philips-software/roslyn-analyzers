// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
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
		private static readonly HashSet<string> _cultureParameterTypes = new HashSet<string>()
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
			MemberAccessExpressionSyntax memberAccessExpressionSyntax = invocationExpressionSyntax.Expression as MemberAccessExpressionSyntax;
			if (memberAccessExpressionSyntax == null || memberAccessExpressionSyntax.Name.ToString() != TryParseMethodName)
			{
				return;
			}

			// If the invoked method contains an IFormatProvider parameter, stop analyzing.
			IMethodSymbol invokedMethod = context.SemanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol as IMethodSymbol;
			if (invokedMethod == null || HasCultureParameter(invokedMethod))
			{
				return;
			}

			// Only display an error if the class implements an overload of TryParse that accepts IFormatProvider.
			IMethodSymbol methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol as IMethodSymbol;
			if (methodSymbol == null)
			{
				return;
			}

			ImmutableArray<ISymbol> members = methodSymbol.ContainingType.GetMembers();
			IEnumerable<ISymbol> tryParseOverloads = members.Where(x => x.Name.StartsWith(TryParseMethodName));

			foreach (ISymbol member in tryParseOverloads)
			{
				IMethodSymbol method = member as IMethodSymbol;
				if (method != null && HasCultureParameter(method))
				{
					// There is an overload that can accept culture as a parameter. Display an error.
					Diagnostic diagnostic = Diagnostic.Create(Rule, invocationExpressionSyntax.GetLocation());
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

		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidTryParseWithoutCulture), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

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
